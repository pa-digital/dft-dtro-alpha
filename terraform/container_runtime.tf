resource "google_service_account" "cloud_run" {
  account_id   = "${var.application_name}-cloud-run"
  display_name = "${var.application_name}-cloud-run"
  description  = "Service Account for ${var.application_name} to run in Cloud Run"
}

locals {
  cloud_run_account = "serviceAccount:${google_service_account.cloud_run.email}"
}

resource "google_secret_manager_secret" "postgres_password" {
  secret_id = "${var.application_name}-postgres-password"

  replication {
    user_managed {
      # Single region for prototype configuration
      replicas {
        location = var.region
      }
    }
  }
}

resource "google_secret_manager_secret_version" "postgres_password_value" {
  secret      = google_secret_manager_secret.postgres_password.id
  secret_data = module.postgres_db.generated_user_password
}

resource "google_secret_manager_secret" "postgres_client_certificate" {
  secret_id = "${var.application_name}-postgres-client-certificate"

  replication {
    user_managed {
      # Single region for prototype configuration
      replicas {
        location = var.region
      }
    }
  }
}

resource "google_secret_manager_secret_version" "postgres_certificate_value" {
  secret      = google_secret_manager_secret.postgres_client_certificate.id
  secret_data = google_sql_ssl_cert.db_client_cert.cert
}

resource "google_secret_manager_secret" "postgres_client_key" {
  secret_id = "${var.application_name}-postgres-client-key"

  replication {
    user_managed {
      # Single region for prototype configuration
      replicas {
        location = var.region
      }
    }
  }
}

resource "google_secret_manager_secret_version" "postgres_key_value" {
  secret      = google_secret_manager_secret.postgres_client_key.id
  secret_data = google_sql_ssl_cert.db_client_cert.private_key
}

# Policies
resource "google_project_iam_member" "storage_bucket_objects" {
  project = var.project
  role    = "roles/storage.objectAdmin"
  member  = local.cloud_run_account

  condition {
    title      = "allow-storage-bucket"
    expression = "resource.name.startsWith(\"projects/_/buckets/${google_storage_bucket.bucket.name}\")"
  }
}

resource "google_project_iam_member" "storage_firestore" {
  project = var.project
  role    = "roles/datastore.user"
  member  = local.cloud_run_account
}

resource "google_project_iam_member" "logging" {
  project = var.project
  role    = "roles/logging.logWriter"
  member  = local.cloud_run_account
}

resource "google_secret_manager_secret_iam_member" "cloud_run_secrets" {
  for_each = { for i, value in [
    google_secret_manager_secret.postgres_password,
    google_secret_manager_secret.postgres_client_certificate,
    google_secret_manager_secret.postgres_client_key,
  ] : i => value }

  project   = var.project
  secret_id = each.value.secret_id
  role      = "roles/secretmanager.secretAccessor"
  member    = local.cloud_run_account
}

data "google_iam_policy" "noauth" {
  binding {
    role = "roles/run.invoker"
    members = [
      "allUsers",
    ]
  }
}

locals {
  db_connection_envs = {
    Postgres__Host   = module.postgres_db.private_ip_address
    Postgres__Port   = "5432",
    Postgres__User   = var.application_name
    Postgres__DbName = local.database_name
    Postgres__UseSsl = true
    PGSSLCERT        = "/secrets/postgres-cert/value"
    PGSSLKEY         = "/secrets/postgres-key/value"
  }
  db_password_env_name = "Postgres__Password"

  db_connection_secret_files = {
    secret_postgres_client_certificate = {
      secret      = google_secret_manager_secret.postgres_client_certificate.secret_id,
      mount_point = "/secrets/postgres-cert"
    },
    secret_postgres_client_key = {
      secret      = google_secret_manager_secret.postgres_client_key.secret_id,
      mount_point = "/secrets/postgres-key"
    }
  }

  common_service_envs = merge(local.db_connection_envs, {
    DEPLOYED = timestamp()

    PROJECTID        = var.project
    BUCKETNAME       = "${random_id.bucket_prefix.hex}-dtro-storage-bucket"
    SEARCHSERVICEURL = "https://${var.search_service_domain}"

    WriteToBucket = var.feature_write_to_bucket
  })
}

# Publish service
resource "google_cloud_run_service_iam_policy" "publish_service_noauth" {
  location = google_cloud_run_v2_service.publish_service.location
  project  = google_cloud_run_v2_service.publish_service.project
  service  = google_cloud_run_v2_service.publish_service.name

  policy_data = data.google_iam_policy.noauth.policy_data
}

resource "google_cloud_run_v2_service" "publish_service" {
  name     = var.publish_service_image
  location = var.region
  ingress  = "INGRESS_TRAFFIC_INTERNAL_LOAD_BALANCER"

  template {
    service_account = google_service_account.cloud_run.email

    vpc_access {
      connector = google_vpc_access_connector.serverless_connector.id
      egress    = "PRIVATE_RANGES_ONLY"
    }

    containers {
      image = "${var.region}-docker.pkg.dev/${var.project}/dtro/${var.publish_service_image}:${var.tag}"

      dynamic "env" {
        for_each = local.common_service_envs
        content {
          name  = env.key
          value = env.value
        }
      }

      env {
        name = local.db_password_env_name
        value_source {
          secret_key_ref {
            secret  = google_secret_manager_secret.postgres_password.secret_id
            version = "latest"
          }
        }
      }

      dynamic "volume_mounts" {
        for_each = local.db_connection_secret_files
        content {
          name       = volume_mounts.key
          mount_path = volume_mounts.value.mount_point
        }
      }

      startup_probe {
        period_seconds    = 4
        failure_threshold = 5

        http_get {
          path = "/health"
          port = 8080
        }
      }

      liveness_probe {
        http_get {
          path = "/health"
          port = 8080
        }
      }
    }

    dynamic "volumes" {
      for_each = local.db_connection_secret_files
      content {
        name = volumes.key
        secret {
          secret       = volumes.value.secret
          default_mode = 0444
          items {
            version = "latest"
            path    = "value"
            mode    = 0400
          }
        }
      }
    }
  }

  depends_on = [
    null_resource.docker_build,
    # Access to secrets is required to start the container
    google_secret_manager_secret_iam_member.cloud_run_secrets,
  ]
}

# Search service
resource "google_cloud_run_service_iam_policy" "search_service_noauth" {
  location = google_cloud_run_v2_service.search_service.location
  project  = google_cloud_run_v2_service.search_service.project
  service  = google_cloud_run_v2_service.search_service.name

  policy_data = data.google_iam_policy.noauth.policy_data
}

resource "google_cloud_run_v2_service" "search_service" {
  name     = var.search_service_image
  location = var.region
  ingress  = "INGRESS_TRAFFIC_INTERNAL_LOAD_BALANCER"

  template {
    service_account = google_service_account.cloud_run.email

    vpc_access {
      connector = google_vpc_access_connector.serverless_connector.id
      egress    = "PRIVATE_RANGES_ONLY"
    }

    containers {
      image = "${var.region}-docker.pkg.dev/${var.project}/dtro/${var.search_service_image}:${var.tag}"

      dynamic "env" {
        for_each = local.common_service_envs
        content {
          name  = env.key
          value = env.value
        }
      }

      env {
        name = local.db_password_env_name
        value_source {
          secret_key_ref {
            secret  = google_secret_manager_secret.postgres_password.secret_id
            version = "latest"
          }
        }
      }

      dynamic "volume_mounts" {
        for_each = local.db_connection_secret_files
        content {
          name       = volume_mounts.key
          mount_path = volume_mounts.value.mount_point
        }
      }

      startup_probe {
        period_seconds    = 4
        failure_threshold = 5

        http_get {
          path = "/health"
          port = 8080
        }
      }

      liveness_probe {
        http_get {
          path = "/health"
          port = 8080
        }
      }
    }

    dynamic "volumes" {
      for_each = local.db_connection_secret_files
      content {
        name = volumes.key
        secret {
          secret       = volumes.value.secret
          default_mode = 0444
          items {
            version = "latest"
            path    = "value"
            mode    = 0400
          }
        }
      }
    }
  }

  depends_on = [
    null_resource.docker_build,
    # Access to secrets is required to start the container
    google_secret_manager_secret_iam_member.cloud_run_secrets,
  ]
}

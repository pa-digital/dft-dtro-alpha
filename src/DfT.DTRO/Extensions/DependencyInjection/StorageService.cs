using System;
using System.Collections.Generic;
using System.Linq;
using DfT.DTRO.DAL;
using DfT.DTRO.Services.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace DfT.DTRO.Extensions.DependencyInjection;

/// <summary>
/// Provides extension methods for injecting <see cref="IStorageService"/> implementations.
/// </summary>
public static class StorageServiceDIExtensions
{
    /// <summary>
    /// Adds a <see cref="MultiStorageService"/> to this service collection as <see cref="IStorageService"/>,
    /// that will delegate its operations to the types provided in <paramref name="primaryImplementer"/>
    /// and optionally to <paramref name="secondaryImplementers"/>
    /// <br/><br/>
    /// If the types delegated to are not already registered,
    /// they will be registered with <see cref="ServiceLifetime.Scoped"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
    /// <param name="primaryImplementer">
    /// The primary type <see cref="MultiStorageService"/> will delegate to.
    /// </param>
    /// <param name="secondaryImplementers">
    /// The other types that the <see cref="MultiStorageService"/> will delegate to.
    /// </param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="primaryImplementer"/> was null.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// At least one of them does not implement <see cref="IStorageService"/>
    /// </exception>
    public static IServiceCollection AddMultiStorageService(this IServiceCollection services, bool writeToBucketOnly, Type primaryImplementer, params Type[] secondaryImplementers)
    {
        if (primaryImplementer is null)
        {
            throw new ArgumentNullException(nameof(primaryImplementer));
        }

        var implementerTypes = new List<Type> { primaryImplementer };
        implementerTypes.AddRange(secondaryImplementers);

        if (implementerTypes.Where(it => !it.GetInterfaces().Contains(typeof(IStorageService)))
                            .FirstOrDefault() is Type invalidImpl)
        {
            throw new InvalidOperationException($"Type '{invalidImpl.FullName}' does not implement '{nameof(IStorageService)}'");
        }

        foreach (var impl in implementerTypes)
        {
            services.TryAddScoped(impl);
        }

        services.AddScoped<IEnumerable<IStorageService>>(s =>
        {
            var list = new List<IStorageService>();
            foreach (var impl in implementerTypes)
            {
                list.Add(s.GetService(impl) as IStorageService);
            }
            return list;
        });

        return services.AddScoped<IStorageService, MultiStorageService>(
            s => new MultiStorageService(
                s.GetService<IEnumerable<IStorageService>>(),
                s.GetService<ILogger<MultiStorageService>>(),
                writeToBucketOnly)
            );
    }

    /// <summary>
    /// Adds a <see cref="MultiStorageService"/> to this service collection as <see cref="IStorageService"/>,
    /// that will delegate its operations to <typeparamref name="TImpl"/>.
    /// <br/><br/>
    /// If <typeparamref name="TImpl"/> is not already registered,
    /// it will be registered with <see cref="ServiceLifetime.Scoped"/>.
    /// </summary>
    /// <typeparam name="TImpl">The type <see cref="MultiStorageService"/> will delegate to.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public static IServiceCollection AddMultiStorageService<TImpl>(this IServiceCollection services, bool writeToBucketOnly = false)
        where TImpl : class, IStorageService
    {
        services.TryAddScoped<TImpl>();

        services.AddScoped<IEnumerable<IStorageService>>(s =>
        {
            var list = new List<IStorageService> { s.GetService<TImpl>() };
            return list;
        });

        return services.AddScoped<IStorageService, MultiStorageService>(
            s => new MultiStorageService(
                s.GetService<IEnumerable<IStorageService>>(),
                s.GetService<ILogger<MultiStorageService>>(),
                writeToBucketOnly)
            );
    }

    /// <summary>
    /// Adds a <see cref="MultiStorageService"/> to this service collection as <see cref="IStorageService"/>,
    /// that will delegate its operations to <typeparamref name="TPrimary"/> and <typeparamref name="TSecondary"/>.
    /// <br/><br/>
    /// If the types delegated to are not already registered,
    /// they will be registered with <see cref="ServiceLifetime.Scoped"/>.
    /// </summary>
    /// <typeparam name="TPrimary">The primary type <see cref="MultiStorageService"/> will delegate to.</typeparam>
    /// <typeparam name="TSecondary">The secondary type that the <see cref="MultiStorageService"/> will delegate to.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public static IServiceCollection AddMultiStorageService<TPrimary, TSecondary>(this IServiceCollection services, bool writeToBucketOnly = false)
        where TPrimary : class, IStorageService
        where TSecondary : class, IStorageService
    {
        services.TryAddScoped<TPrimary>();
        services.TryAddScoped<TSecondary>();

        services.AddScoped<IEnumerable<IStorageService>>(s =>
        {
            var list = new List<IStorageService> { s.GetService<TPrimary>(), s.GetService<TSecondary>() };
            return list;
        });

        return services.AddScoped<IStorageService, MultiStorageService>(
            s => new MultiStorageService(
                s.GetService<IEnumerable<IStorageService>>(),
                s.GetService<ILogger<MultiStorageService>>(),
                writeToBucketOnly)
            );
    }

    // Add more of those if needed...

    /// <summary>
    /// Adds <see cref="DtroContext"/> with PostgreSQL
    /// backend using the provided connection string.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
    /// <param name="connectionString">The connection string to the database.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public static IServiceCollection AddPostgresDtroContext(this IServiceCollection services, string connectionString)
    {
        return services.AddDbContext<DtroContext>(
            opt => opt.UseNpgsql(connectionString)
        );
    }

    /// <summary>
    /// Adds <see cref="DtroContext"/> with PostgreSQL
    /// backend using the provided connection string.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
    /// <param name="host">The address of the database host.</param>
    /// <param name="user">The username used to log in.</param>
    /// <param name="password">The password used to log in.</param>
    /// <param name="useSsl">Enables encryption of connection with SSL/TLS.</param>
    /// <param name="database">The name of the database. By default the same as <paramref name="user"/>.</param>
    /// <param name="port">The port used to connect to the database. By defauls <c>5432</c>.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public static IServiceCollection AddPostgresDtroContext(
        this IServiceCollection services,
        string host,
        string user,
        string password,
        bool useSsl,
        string database = null,
        int port = 5432)
        => services.AddPostgresDtroContext(
            BuildPostgresConnectionString(host, user, password, useSsl, database, port
            )
        );

    /// <summary>
    /// Adds <see cref="SqlStorageService"/> as <see cref="IStorageService"/>
    /// with PostgreSQL backend using the provided connection string.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
    /// <param name="connectionString">The connection string to the database.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public static IServiceCollection AddPostgresStorage(this IServiceCollection services, string connectionString)
    {
        services.AddPostgresDtroContext(connectionString);
        return services.AddScoped<IStorageService, SqlStorageService>();
    }

    /// <summary>
    /// Adds <see cref="SqlStorageService"/> as <see cref="IStorageService"/>
    /// with PostgreSQL backend using the provided connection parameters.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
    /// <param name="host">The address of the database host.</param>
    /// <param name="user">The username used to log in.</param>
    /// <param name="password">The password used to log in.</param>
    /// <param name="useSsl">Enables encryption of connection with SSL/TLS.</param>
    /// <param name="database">The name of the database. By default the same as <paramref name="user"/>.</param>
    /// <param name="port">The port used to connect to the database. By defauls <c>5432</c>.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public static IServiceCollection AddPostgresStorage(
        this IServiceCollection services,
        string host,
        string user,
        string password,
        bool useSsl,
        string database = null,
        int port = 5432)
        => services.AddPostgresStorage(
            BuildPostgresConnectionString(
                host, user, password, useSsl, database, port
            )
        );

    /// <summary>
    /// Adds an implementation of <see cref="IStorageService"/>.
    /// The specific implementation depends on what is described in the provided configuration.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
    /// <param name="configuration">The configuration that the storage service is infered from.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public static IServiceCollection AddStorage(this IServiceCollection services, IConfiguration configuration)
    {
        var postgresConfig = configuration.GetRequiredSection("Postgres");

        var host = postgresConfig.GetValue<string>("Host");
        var port = postgresConfig.GetValue<int?>("Port") ?? 5432;
        var user = postgresConfig.GetValue<string>("User");
        var password = postgresConfig.GetValue<string>("Password");
        var database = postgresConfig.GetValue<string>("DbName");
        var useSsl = postgresConfig.GetValue("UseSsl", false);

        if (configuration.GetValue<bool?>("WriteToBucket") ?? false)
        {
            return
                services
                    .AddPostgresDtroContext(host, user, password, useSsl, database, port)
                    .AddMultiStorageService<SqlStorageService, FileStorageService>();
        }

        return services.AddPostgresStorage(host, user, password, useSsl, database, port);
    }

    private static string BuildPostgresConnectionString(
        string host,
        string user,
        string password,
        bool useSsl,
        string database = null,
        int port = 5432
    )
        => $"Host={host ?? throw new ArgumentNullException(nameof(host))}:{port};" +
           $"Username={user ?? throw new ArgumentNullException(nameof(user))};" +
           $"Password={password ?? throw new ArgumentNullException(nameof(password))};" +
           $"Database={database ?? user};" +
           $"{(useSsl ? "sslmode=Require;Trust Server Certificate=true;" : "")};";
}
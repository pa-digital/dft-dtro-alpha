using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using DfT.DTRO.JsonLogic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace DfT.DTRO.Services.Validation;

/// <inheritdoc/>
public class JsonLogicValidationService : IJsonLogicValidationService
{
    private readonly IJsonLogicRuleSource _ruleSource;

    /// <summary>
    /// Default constructor.
    /// </summary>
    /// <param name="ruleSource">The <see cref="IJsonLogicRuleSource"/> used to retrieve the rules.</param>
    public JsonLogicValidationService(IJsonLogicRuleSource ruleSource)
    {
        _ruleSource = ruleSource;
    }

    /// <inheritdoc/>
    public async Task<IList<SemanticValidationError>> ValidateCreationRequest(Models.DTRO request)
    {
        if (request.SchemaVersion < "3.1.2")
        {
            return new List<SemanticValidationError>();
        }

        var rules = await _ruleSource.GetRules($"dtro-{request.SchemaVersion}");

        var errors = new List<SemanticValidationError>();

        var json = JsonConvert.SerializeObject(request.Data, new ExpandoObjectConverter());
        var node = JsonNode.Parse(json);

        foreach (var rule in rules)
        {
            var result = rule.Rule.Apply(node);
            if (result.AsValue().TryGetValue(out bool value) && !value)
            {
                errors.Add(new SemanticValidationError()
                {
                    Message = rule.Message,
                    Path = rule.Path
                });
            }
        }

        return errors;
    }
}

using Cfa.ACHInterbank.Domain.Models.ACH.Config;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

internal static class NachaCompleteProfileQuery
{
    internal static IQueryable<CfgProfile> Create(AchDbContext context)
        => context.CfgProfiles
            .AsSplitQuery()
            .Include(profile => profile.Status)
            .Include(profile => profile.ClearingHouse)
            .Include(profile => profile.FlowType)
            .Include(profile => profile.Direction)
            .Include(profile => profile.ServiceClass)
            .Include(profile => profile.Tags)
            .Include(profile => profile.Records)
                .ThenInclude(record => record.RecordCode)
            .Include(profile => profile.Records)
                .ThenInclude(record => record.LayoutVariant)
            .Include(profile => profile.Records)
                .ThenInclude(record => record.SemanticRuleSet)
                    .ThenInclude(ruleSet => ruleSet!.Rules)
                        .ThenInclude(rule => rule.RuleType)
            .Include(profile => profile.LayoutVariants)
                .ThenInclude(variant => variant.RecordCode)
            .Include(profile => profile.LayoutVariants)
                .ThenInclude(variant => variant.Status)
            .Include(profile => profile.LayoutVariants)
                .ThenInclude(variant => variant.Fields)
                    .ThenInclude(field => field.SourceDefinition)
                        .ThenInclude(source => source.DataSourceType)
            .Include(profile => profile.LayoutVariants)
                .ThenInclude(variant => variant.Fields)
                    .ThenInclude(field => field.Rules)
                        .ThenInclude(rule => rule.RuleType);
}

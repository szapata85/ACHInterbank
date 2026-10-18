using Cfa.ACHInterbank.Application.Integrations.Interfaces;

namespace Cfa.ACHInterbank.Application.Integrations.Transformations;

public static class StandardIntegrationTransformationCatalog
{
    public static IReadOnlyCollection<IIntegrationTransformation> Create()
        =>
        [
            new TrimIntegrationTransformation(),
            new UppercaseIntegrationTransformation(),
            new LowercaseIntegrationTransformation(),
            new PadLeftIntegrationTransformation(),
            new PadRightIntegrationTransformation(),
            new SubstringIntegrationTransformation(),
            new ConcatIntegrationTransformation(),
            new DateFormatIntegrationTransformation(),
            new NumericFormatIntegrationTransformation(),
            new NullIfEmptyIntegrationTransformation(),
            new DefaultIfNullIntegrationTransformation()
        ];
}

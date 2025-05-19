using System.Activities.Presentation.Metadata;
using System.ComponentModel;
using System.ComponentModel.Design;
using MaincraftRulez.Anonymizator.Activities.Design.Designers;
using MaincraftRulez.Anonymizator.Activities.Design.Properties;

namespace MaincraftRulez.Anonymizator.Activities.Design
{
    public class DesignerMetadata : IRegisterMetadata
    {
        public void Register()
        {
            var builder = new AttributeTableBuilder();
            builder.ValidateTable();

            var categoryAttribute = new CategoryAttribute($"{Resources.Category}");

            builder.AddCustomAttributes(typeof(UseAnonymization), categoryAttribute);
            builder.AddCustomAttributes(typeof(UseAnonymization), new DesignerAttribute(typeof(UseAnonymizationDesigner)));
            builder.AddCustomAttributes(typeof(UseAnonymization), new HelpKeywordAttribute(""));

            builder.AddCustomAttributes(typeof(TextAnonymization), categoryAttribute);
            builder.AddCustomAttributes(typeof(TextAnonymization), new DesignerAttribute(typeof(TextAnonymizationDesigner)));
            builder.AddCustomAttributes(typeof(TextAnonymization), new HelpKeywordAttribute(""));

            builder.AddCustomAttributes(typeof(TextDeanonymization), categoryAttribute);
            builder.AddCustomAttributes(typeof(TextDeanonymization), new DesignerAttribute(typeof(TextDeanonymizationDesigner)));
            builder.AddCustomAttributes(typeof(TextDeanonymization), new HelpKeywordAttribute(""));

            builder.AddCustomAttributes(typeof(GenerateSecretKey), categoryAttribute);
            builder.AddCustomAttributes(typeof(GenerateSecretKey), new DesignerAttribute(typeof(GenerateSecretKeyDesigner)));
            builder.AddCustomAttributes(typeof(GenerateSecretKey), new HelpKeywordAttribute(""));

            builder.AddCustomAttributes(typeof(PhoneAnonymization), categoryAttribute);
            builder.AddCustomAttributes(typeof(PhoneAnonymization), new DesignerAttribute(typeof(PhoneAnonymizationDesigner)));
            builder.AddCustomAttributes(typeof(PhoneAnonymization), new HelpKeywordAttribute(""));

            builder.AddCustomAttributes(typeof(PhoneDeanonymization), categoryAttribute);
            builder.AddCustomAttributes(typeof(PhoneDeanonymization), new DesignerAttribute(typeof(PhoneDeanonymizationDesigner)));
            builder.AddCustomAttributes(typeof(PhoneDeanonymization), new HelpKeywordAttribute(""));


            MetadataStore.AddAttributeTable(builder.CreateTable());
        }
    }
}

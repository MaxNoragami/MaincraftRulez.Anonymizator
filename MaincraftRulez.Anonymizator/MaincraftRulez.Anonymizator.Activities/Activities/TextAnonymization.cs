using System;
using System.Activities;
using System.Threading;
using System.Threading.Tasks;
using MaincraftRulez.Anonymizator.Activities.Properties;
using UiPath.Shared.Activities;
using UiPath.Shared.Activities.Localization;
using MaincraftRulez.Anonymizator;
using UiPath.Shared.Activities.Utilities;

namespace MaincraftRulez.Anonymizator.Activities
{
    [LocalizedDisplayName(nameof(Resources.TextAnonymization_DisplayName))]
    [LocalizedDescription(nameof(Resources.TextAnonymization_Description))]
    public class TextAnonymization : ContinuableAsyncCodeActivity
    {
        #region Properties

        /// <summary>
        /// If set, continue executing the remaining activities even if the current activity has failed.
        /// </summary>
        [LocalizedCategory(nameof(Resources.Common_Category))]
        [LocalizedDisplayName(nameof(Resources.ContinueOnError_DisplayName))]
        [LocalizedDescription(nameof(Resources.ContinueOnError_Description))]
        public override InArgument<bool> ContinueOnError { get; set; }

        [LocalizedCategory(nameof(Resources.Common_Category))]
        [LocalizedDisplayName(nameof(Resources.Timeout_DisplayName))]
        [LocalizedDescription(nameof(Resources.Timeout_Description))]
        public InArgument<int> TimeoutMS { get; set; } = 60000;

        [LocalizedDisplayName(nameof(Resources.TextAnonymization_OriginalText_DisplayName))]
        [LocalizedDescription(nameof(Resources.TextAnonymization_OriginalText_Description))]
        [LocalizedCategory(nameof(Resources.Input_Category))]
        public InArgument<string> OriginalText { get; set; }

        [LocalizedDisplayName(nameof(Resources.TextAnonymization_AnonymizedText_DisplayName))]
        [LocalizedDescription(nameof(Resources.TextAnonymization_AnonymizedText_Description))]
        [LocalizedCategory(nameof(Resources.Output_Category))]
        public OutArgument<string> AnonymizedText { get; set; }

        #endregion

        #region Constructors

        public TextAnonymization()
        {
        }

        #endregion

        #region Protected Methods

        protected override void CacheMetadata(CodeActivityMetadata metadata)
        {
            if (OriginalText == null) metadata.AddValidationError(string.Format(Resources.ValidationValue_Error, nameof(OriginalText)));

            // Validate that this activity is used within a UseAnonymization scope
            //metadata.AddValidationError(
            //    string.Format(Resources.ValidationScope_Error, nameof(UseAnonymization)));

            base.CacheMetadata(metadata);
        }

        protected override async Task<Action<AsyncCodeActivityContext>> ExecuteAsync(AsyncCodeActivityContext context, CancellationToken cancellationToken)
        {
            // Inputs
            var timeout = TimeoutMS.Get(context);
            var originalText = OriginalText.Get(context);
            string anonymizedText = string.Empty;

            // Set a timeout on the execution
            var task = ExecuteWithTimeout(context, originalText, cancellationToken);
            if (await Task.WhenAny(task, Task.Delay(timeout, cancellationToken)) != task) throw new TimeoutException(Resources.Timeout_Error);
            anonymizedText = await task;

            // Outputs
            return (ctx) => {
                AnonymizedText.Set(ctx, anonymizedText);
            };
        }

        private async Task<string> ExecuteWithTimeout(AsyncCodeActivityContext context, string originalText, CancellationToken cancellationToken = default)
        {
            try
            {
                // Get the cipher directly from the UseAnonymization class
                var cipher = UseAnonymization.GetCipher();

                if (cipher == null)
                    throw new InvalidOperationException("This activity must be used within a UseAnonymization scope.");

                // Create a string anonymizer using the cipher
                var anonymizer = new StringAnonymizer(cipher);

                // Customize settings as needed
                anonymizer.SetPreserveCase(true);
                anonymizer.SetPreserveSpaces(true);
                anonymizer.SetPreservePunctuation(true);

                // Anonymize the text
                string anonymizedText = anonymizer.Anonymize(originalText);

                return await Task.FromResult(anonymizedText);
            }
            catch (Exception ex)
            {
                // Add more informative error message to help troubleshoot
                throw new InvalidOperationException("Error anonymizing text. Make sure this activity is used within a UseAnonymization scope and the key is valid.", ex);
            }
        }

        #endregion
    }
}
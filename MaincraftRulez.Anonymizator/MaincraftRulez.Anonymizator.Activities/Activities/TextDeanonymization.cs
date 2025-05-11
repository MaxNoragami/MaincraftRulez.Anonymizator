using System;
using System.Activities;
using System.Threading;
using System.Threading.Tasks;
using MaincraftRulez.Anonymizator.Activities.Properties;
using UiPath.Shared.Activities;
using UiPath.Shared.Activities.Localization;

namespace MaincraftRulez.Anonymizator.Activities
{
    [LocalizedDisplayName(nameof(Resources.TextDeanonymization_DisplayName))]
    [LocalizedDescription(nameof(Resources.TextDeanonymization_Description))]
    public class TextDeanonymization : ContinuableAsyncCodeActivity
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

        [LocalizedDisplayName(nameof(Resources.TextDeanonymization_AnonymizedText_DisplayName))]
        [LocalizedDescription(nameof(Resources.TextDeanonymization_AnonymizedText_Description))]
        [LocalizedCategory(nameof(Resources.Input_Category))]
        public InArgument<string> AnonymizedText { get; set; }

        [LocalizedDisplayName(nameof(Resources.TextDeanonymization_OriginalText_DisplayName))]
        [LocalizedDescription(nameof(Resources.TextDeanonymization_OriginalText_Description))]
        [LocalizedCategory(nameof(Resources.Output_Category))]
        public OutArgument<string> OriginalText { get; set; }

        #endregion


        #region Constructors

        public TextDeanonymization()
        {
        }

        #endregion


        #region Protected Methods

        protected override void CacheMetadata(CodeActivityMetadata metadata)
        {
            if (AnonymizedText == null) metadata.AddValidationError(string.Format(Resources.ValidationValue_Error, nameof(AnonymizedText)));

            base.CacheMetadata(metadata);
        }

        protected override async Task<Action<AsyncCodeActivityContext>> ExecuteAsync(AsyncCodeActivityContext context, CancellationToken cancellationToken)
        {
            // Inputs
            var timeout = TimeoutMS.Get(context);
            var anonymizedText = AnonymizedText.Get(context);

            // Set a timeout on the execution
            var task = ExecuteWithTimeout(context, cancellationToken);
            if (await Task.WhenAny(task, Task.Delay(timeout, cancellationToken)) != task) throw new TimeoutException(Resources.Timeout_Error);

            // Outputs
            return (ctx) => {
                OriginalText.Set(ctx, null);
            };
        }

        private async Task ExecuteWithTimeout(AsyncCodeActivityContext context, CancellationToken cancellationToken = default)
        {
            ///////////////////////////
            // Add execution logic HERE
            ///////////////////////////
        }

        #endregion
    }
}


using System;
using System.Activities;
using System.Threading;
using System.Threading.Tasks;
using MaincraftRulez.Anonymizator.Activities.Properties;
using UiPath.Shared.Activities;
using UiPath.Shared.Activities.Localization;

namespace MaincraftRulez.Anonymizator.Activities
{
    [LocalizedDisplayName(nameof(Resources.PhoneDeanonymization_DisplayName))]
    [LocalizedDescription(nameof(Resources.PhoneDeanonymization_Description))]
    public class PhoneDeanonymization : ContinuableAsyncCodeActivity
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

        [LocalizedDisplayName(nameof(Resources.PhoneDeanonymization_AnonymizedPhoneNumber_DisplayName))]
        [LocalizedDescription(nameof(Resources.PhoneDeanonymization_AnonymizedPhoneNumber_Description))]
        [LocalizedCategory(nameof(Resources.Input_Category))]
        public InArgument<string> AnonymizedPhoneNumber { get; set; }

        [LocalizedDisplayName(nameof(Resources.PhoneDeanonymization_OriginalPhoneNumber_DisplayName))]
        [LocalizedDescription(nameof(Resources.PhoneDeanonymization_OriginalPhoneNumber_Description))]
        [LocalizedCategory(nameof(Resources.Output_Category))]
        public OutArgument<string> OriginalPhoneNumber { get; set; }

        [LocalizedDisplayName(nameof(Resources.PhoneDeanonymization_PreserveCountryCode_DisplayName))]
        [LocalizedDescription(nameof(Resources.PhoneDeanonymization_PreserveCountryCode_Description))]
        [LocalizedCategory(nameof(Resources.Input_Category))]
        public InArgument<bool> PreserveCountryCode { get; set; }

        [LocalizedDisplayName(nameof(Resources.PhoneDeanonymization_PreserveAreaCode_DisplayName))]
        [LocalizedDescription(nameof(Resources.PhoneDeanonymization_PreserveAreaCode_Description))]
        [LocalizedCategory(nameof(Resources.Input_Category))]
        public InArgument<bool> PreserveAreaCode { get; set; }

        [LocalizedDisplayName(nameof(Resources.PhoneDeanonymization_PreserveLeadingDigits_DisplayName))]
        [LocalizedDescription(nameof(Resources.PhoneDeanonymization_PreserveLeadingDigits_Description))]
        [LocalizedCategory(nameof(Resources.Input_Category))]
        public InArgument<int> PreserveLeadingDigits { get; set; }

        #endregion


        #region Constructors

        public PhoneDeanonymization()
        {
        }

        #endregion


        #region Protected Methods

        protected override void CacheMetadata(CodeActivityMetadata metadata)
        {
            if (AnonymizedPhoneNumber == null) metadata.AddValidationError(string.Format(Resources.ValidationValue_Error, nameof(AnonymizedPhoneNumber)));

            base.CacheMetadata(metadata);
        }

        protected override async Task<Action<AsyncCodeActivityContext>> ExecuteAsync(AsyncCodeActivityContext context, CancellationToken cancellationToken)
        {
            // Inputs
            var timeout = TimeoutMS.Get(context);
            var anonymizedPhoneNumber = AnonymizedPhoneNumber.Get(context);
            var preserveCountryCode = PreserveCountryCode.Get(context);
            var preserveAreaCode = PreserveAreaCode.Get(context);
            var preserveLeadingDigits = PreserveLeadingDigits.Get(context);
            string originalPhoneNumber = string.Empty;

            // Set a timeout on the execution
            var task = ExecuteWithTimeout(context, anonymizedPhoneNumber, preserveCountryCode, preserveAreaCode, preserveLeadingDigits, cancellationToken);
            if (await Task.WhenAny(task, Task.Delay(timeout, cancellationToken)) != task) throw new TimeoutException(Resources.Timeout_Error);
            originalPhoneNumber = await task;

            // Outputs
            return (ctx) => {
                OriginalPhoneNumber.Set(ctx, originalPhoneNumber);
            };
        }

        private async Task<string> ExecuteWithTimeout(AsyncCodeActivityContext context, string anonymizedPhoneNumber, bool preserveCountryCode, bool preserveAreaCode, int preserveLeadingDigits, CancellationToken cancellationToken = default)
        {
            try
            {
                // Get the cipher directly from the UseAnonymization class
                var cipher = UseAnonymization.GetCipher();

                if (cipher == null)
                    throw new InvalidOperationException("This activity must be used within a UseAnonymization scope.");

                // Create a phone number anonymizer using the cipher
                var phoneAnonymizer = new PhoneNumberAnonymizer(cipher);

                // Configure the anonymizer with activity properties
                phoneAnonymizer.SetPreserveCountryCode(preserveCountryCode);
                phoneAnonymizer.SetPreserveAreaCode(preserveAreaCode);
                phoneAnonymizer.SetPreserveLeadingDigits(preserveLeadingDigits);

                // Deanonymize the phone number
                string originalPhoneNumber = phoneAnonymizer.Deanonymize(anonymizedPhoneNumber);

                return await Task.FromResult(originalPhoneNumber);
            }
            catch (Exception ex)
            {
                // Add more informative error message to help troubleshoot
                throw new InvalidOperationException("Error deanonymizing phone number. Make sure this activity is used within a UseAnonymization scope and the key is valid.", ex);
            }
        }

        #endregion
    }
}


using System;
using System.Activities;
using System.Threading;
using System.Threading.Tasks;
using System.Security;
using System.Text;
using MaincraftRulez.Anonymizator.Activities.Properties;
using UiPath.Shared.Activities;
using UiPath.Shared.Activities.Localization;
using System.ComponentModel;

namespace MaincraftRulez.Anonymizator.Activities
{
    [LocalizedDisplayName(nameof(Resources.GenerateSecretKey_DisplayName))]
    [LocalizedDescription(nameof(Resources.GenerateSecretKey_Description))]
    public class GenerateSecretKey : ContinuableAsyncCodeActivity
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

        [LocalizedDisplayName("Generate Tweak")]
        [LocalizedDescription("If checked, generates a tweak value instead of a secret key")]
        [LocalizedCategory(nameof(Resources.Options_Category))]
        [DefaultValue(false)]
        public InArgument<bool> GenerateTweak { get; set; } = false;

        [LocalizedDisplayName(nameof(Resources.GenerateSecretKey_GeneratedKey_DisplayName))]
        [LocalizedDescription(nameof(Resources.GenerateSecretKey_GeneratedKey_Description))]
        [LocalizedCategory(nameof(Resources.Output_Category))]
        public OutArgument<SecureString> GeneratedKey { get; set; }

        #endregion


        #region Constructors

        public GenerateSecretKey()
        {
        }

        #endregion


        #region Protected Methods

        protected override void CacheMetadata(CodeActivityMetadata metadata)
        {
            // Add validation to require GeneratedKey output
            if (GeneratedKey == null) metadata.AddValidationError(string.Format(Resources.ValidationValue_Error, nameof(GeneratedKey)));
            
            // No validation needed as GenerateTweak has a default value
            base.CacheMetadata(metadata);
        }

        protected override async Task<Action<AsyncCodeActivityContext>> ExecuteAsync(AsyncCodeActivityContext context, CancellationToken cancellationToken)
        {
            // Inputs
            var timeout = TimeoutMS.Get(context);
            var generateTweak = GenerateTweak.Get(context);
            SecureString secureOutput = null;

            // Set a timeout on the execution
            var task = ExecuteWithTimeout(context, generateTweak, cancellationToken);
            if (await Task.WhenAny(task, Task.Delay(timeout, cancellationToken)) != task) throw new TimeoutException(Resources.Timeout_Error);
            secureOutput = await task;

            // Outputs
            return (ctx) => {
                GeneratedKey.Set(ctx, secureOutput);
            };
        }

        private async Task<SecureString> ExecuteWithTimeout(AsyncCodeActivityContext context, bool generateTweak, CancellationToken cancellationToken = default)
        {
            // Create a key generator instance
            var keyGenerator = new KeyGenerator();
            
            string generatedValue;
            
            // Generate either key or tweak based on boolean flag
            if (generateTweak)
            {
                // Generate a tweak
                var pair = keyGenerator.GenerateKeyTweakPair();
                generatedValue = ByteArrayToHexString(pair.Tweak);
            }
            else
            {
                // Default case: generate a secret key
                var pair = keyGenerator.GenerateKeyTweakPair();
                generatedValue = pair.Key;
            }
            
            // Convert string to SecureString
            var secureString = new SecureString();
            foreach (char c in generatedValue)
            {
                secureString.AppendChar(c);
            }
            secureString.MakeReadOnly();
            
            return await Task.FromResult(secureString);
        }

        // Helper method to convert byte array to hex string
        private string ByteArrayToHexString(byte[] bytes)
        {
            StringBuilder hex = new StringBuilder(bytes.Length * 2);
            foreach (byte b in bytes)
            {
                hex.AppendFormat("{0:x2}", b);
            }
            return hex.ToString();
        }

        #endregion
    }
}


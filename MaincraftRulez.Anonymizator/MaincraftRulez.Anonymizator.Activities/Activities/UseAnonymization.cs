using System;
using System.Activities;
using System.Threading;
using System.Threading.Tasks;
using System.Activities.Statements;
using System.ComponentModel;
using MaincraftRulez.Anonymizator.Activities.Properties;
using UiPath.Shared.Activities;
using UiPath.Shared.Activities.Localization;
using System.Linq;
using System.Security;
using System.Runtime.InteropServices;

namespace MaincraftRulez.Anonymizator.Activities
{
    [LocalizedDisplayName(nameof(Resources.UseAnonymization_DisplayName))]
    [LocalizedDescription(nameof(Resources.UseAnonymization_Description))]
    public class UseAnonymization : ContinuableAsyncNativeActivity
    {
        #region Properties

        [Browsable(false)]
        public ActivityAction<IObjectContainer​> Body { get; set; }

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

        [LocalizedDisplayName(nameof(Resources.UseAnonymization_Key_DisplayName))]
        [LocalizedDescription(nameof(Resources.UseAnonymization_Key_Description))]
        [LocalizedCategory(nameof(Resources.Input_Category))]
        public InArgument<SecureString> Key { get; set; }

        [LocalizedDisplayName(nameof(Resources.UseAnonymization_Tweak_DisplayName))]
        [LocalizedDescription(nameof(Resources.UseAnonymization_Tweak_Description))]
        [LocalizedCategory(nameof(Resources.Input_Category))]
        public InArgument<SecureString> Tweak { get; set; }

        // A tag used to identify the scope in the activity context
        internal static string ParentContainerPropertyTag => "ScopeActivity";

        // Object Container: Add strongly-typed objects here and they will be available in the scope's child activities.
        private readonly IObjectContainer _objectContainer;

        #endregion

        #region Constructors

        public UseAnonymization(IObjectContainer objectContainer)
        {
            _objectContainer = objectContainer;

            Body = new ActivityAction<IObjectContainer>
            {
                Argument = new DelegateInArgument<IObjectContainer>(ParentContainerPropertyTag),
                Handler = new Sequence { DisplayName = Resources.Do }
            };
        }

        public UseAnonymization() : this(new ObjectContainer())
        {

        }

        #endregion

        #region Protected Methods

        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            if (Key == null) metadata.AddValidationError(string.Format(Resources.ValidationValue_Error, nameof(Key)));

            base.CacheMetadata(metadata);
        }

        protected override async Task<Action<NativeActivityContext>> ExecuteAsync(NativeActivityContext context, CancellationToken cancellationToken)
        {
            // Inputs
            var timeout = TimeoutMS.Get(context);
            var secureKey = Key.Get(context);
            var secureTweak = Tweak.Get(context);

            // Set a timeout on the execution
            var task = ExecuteWithTimeout(context, secureKey, secureTweak, cancellationToken);
            if (await Task.WhenAny(task, Task.Delay(timeout, cancellationToken)) != task) throw new TimeoutException(Resources.Timeout_Error);
            await task;

            return (ctx) => {
                // Schedule child activities
                ctx.Properties.Add(ParentContainerPropertyTag, _objectContainer);

                if (Body != null)
                    ctx.ScheduleAction<IObjectContainer>(Body, _objectContainer, OnCompleted, OnFaulted);

                // Outputs
            };
        }

        private static IFF3Cipher _sharedCipher;

        // Add a static method to access the cipher
        public static IFF3Cipher GetCipher()
        {
            return _sharedCipher;
        }

        private async Task ExecuteWithTimeout(NativeActivityContext context, SecureString secureKey, SecureString secureTweak, CancellationToken cancellationToken = default)
        {
            try {
                // Convert SecureString key to regular string (within this method only)
                string key = SecureStringToString(secureKey);

                // Convert hex key to bytes
                byte[] keyBytes = StringToByteArray(key);

                // If tweak is provided, convert it to bytes, otherwise use default tweak (zeros)
                byte[] tweakBytes;
                if (secureTweak != null && secureTweak.Length > 0)
                {
                    string tweak = SecureStringToString(secureTweak);
                    tweakBytes = StringToByteArray(tweak);
                }
                else
                {
                    tweakBytes = new byte[7] { 0, 0, 0, 0, 0, 0, 0 }; // FF3-1 uses 7-byte tweak
                }

                // Create cipher with key and tweak
                var cipher = new FF3Cipher(key, tweakBytes);

                // Store in the static field for child activities to access
                _sharedCipher = cipher;

                // Also add to the container as before (for backward compatibility)
                _objectContainer.Add<IFF3Cipher>(cipher);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error initializing anonymization. Please ensure the key and tweak are valid hex strings.", ex);
            }

            await Task.CompletedTask;
        }

        // Helper method to convert SecureString to string
        private static string SecureStringToString(SecureString secureString)
        {
            if (secureString == null)
                return null;

            IntPtr valuePtr = IntPtr.Zero;
            try
            {
                valuePtr = Marshal.SecureStringToGlobalAllocUnicode(secureString);
                return Marshal.PtrToStringUni(valuePtr);
            }
            finally
            {
                if (valuePtr != IntPtr.Zero)
                    Marshal.ZeroFreeGlobalAllocUnicode(valuePtr);
            }
        }

        // Add this to cleanup
        protected override void Cancel(NativeActivityContext context)
        {
            base.Cancel(context);
            _sharedCipher = null; // Clean up the shared cipher
        }

        private void OnCompleted(NativeActivityContext context, ActivityInstance completedInstance)
        {
            _sharedCipher = null; // Clean up the shared cipher
            Cleanup();
        }

        private static byte[] StringToByteArray(string hex)
        {
            if (string.IsNullOrEmpty(hex))
                return Array.Empty<byte>();

            // Remove any spaces or hyphens
            hex = hex.Replace(" ", "").Replace("-", "");

            int len = hex.Length;
            byte[] bytes = new byte[len / 2];

            for (int i = 0; i < len; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }

            return bytes;
        }

        #endregion

        #region Events

        private void OnFaulted(NativeActivityFaultContext faultContext, Exception propagatedException, ActivityInstance propagatedFrom)
        {
            faultContext.CancelChildren();
            Cleanup();
        }

        

        #endregion

        #region Helpers

        private void Cleanup()
        {
            var disposableObjects = _objectContainer.Where(o => o is IDisposable);
            foreach (var obj in disposableObjects)
            {
                if (obj is IDisposable dispObject)
                    dispObject.Dispose();
            }
            _objectContainer.Clear();
        }

        #endregion
    }
}
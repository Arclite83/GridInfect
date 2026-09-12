// R-701/R-702: the single non-consumable, over Unity IAP.
//
// UNVERIFIED AGAINST THE REAL SDK — see the header in AdMobServices.cs. This
// one is gated by GRIDINFECT_IAP, which the Services asmdef defines
// automatically when com.unity.purchasing is in the manifest (a versionDefine,
// so there is no symbol to remember). Unity IAP 5.x reworked the API from 4.x;
// if this does not compile on import, that is the likely reason and the fix is
// mechanical.
//
// Ownership is read from the store's own receipt, never from a local flag: a
// bool in PlayerPrefs is a one-line "unlock everything" for anyone with a file
// browser, and remove-ads is the only thing in this game worth forging.

#if GRIDINFECT_IAP
using System;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;

namespace GridInfect.Services
{
    public sealed class UnityIapPurchaseService : IDetailedStoreListener, IPurchaseService
    {
        public const string RemoveAdsProductId = "remove_ads";

        IStoreController _controller;
        IExtensionProvider _extensions;
        Action _ready;
        Action<bool> _pending;

        public void Initialize(Action ready)
        {
            _ready = ready;
            var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
            builder.AddProduct(RemoveAdsProductId, ProductType.NonConsumable);
            UnityPurchasing.Initialize(this, builder);
        }

        public bool RemoveAdsOwned
        {
            get
            {
                var product = _controller?.products?.WithID(RemoveAdsProductId);
                return product != null && product.hasReceipt;
            }
        }

        public void BuyRemoveAds(Action<bool> owned)
        {
            if (_controller == null) { owned?.Invoke(false); return; }
            if (RemoveAdsOwned) { owned?.Invoke(true); return; }
            _pending = owned;
            _controller.InitiatePurchase(RemoveAdsProductId);
        }

        // R-702: an App Store review requirement, free on Play via receipt
        // query. On Android initialization already restores, so this only has
        // real work to do on iOS.
        public void Restore(Action<bool> owned)
        {
            // Android restores on initialize, so the receipt is already there
            // and there is nothing to ask the store for.
            if (Application.platform != RuntimePlatform.IPhonePlayer || _extensions == null)
            {
                owned?.Invoke(RemoveAdsOwned);
                return;
            }

            _extensions.GetExtension<IAppleExtensions>()
                .RestoreTransactions((success, error) => owned?.Invoke(success && RemoveAdsOwned));
        }

        // --- IStoreListener ---------------------------------------------------

        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            _controller = controller;
            _extensions = extensions;
            _ready?.Invoke();
            _ready = null;
        }

        public void OnInitializeFailed(InitializationFailureReason error) => OnInitializeFailed(error, null);

        public void OnInitializeFailed(InitializationFailureReason error, string message)
        {
            // No store, no purchase, and the game plays on. The NO ADS chip
            // stays visible and its tap is inert, which is honest: the store
            // is what is unavailable, not the product.
            _ready?.Invoke();
            _ready = null;
        }

        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
        {
            bool isRemoveAds = string.Equals(
                args.purchasedProduct.definition.id, RemoveAdsProductId, StringComparison.Ordinal);
            var pending = _pending;
            _pending = null;
            pending?.Invoke(isRemoveAds);
            return PurchaseProcessingResult.Complete;
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureReason reason) =>
            OnPurchaseFailed(product, new PurchaseFailureDescription(
                product != null ? product.definition.id : string.Empty, reason, string.Empty));

        public void OnPurchaseFailed(Product product, PurchaseFailureDescription description)
        {
            var pending = _pending;
            _pending = null;
            pending?.Invoke(false);
        }
    }
}
#endif

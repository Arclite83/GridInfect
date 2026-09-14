// R-701/R-702: the single non-consumable, over Unity IAP.
//
// Checked against the com.unity.purchasing 5.4.3 source (needle-mirror,
// 2026-09-14), not yet compiled in the editor. What was checked: every type
// here is in the UnityEngine.Purchasing namespace and the Unity.Purchasing
// assembly (the asmdef reference); IStoreListener's two OnInitializeFailed
// overloads; IAppleExtensions.RestoreTransactions takes Action<bool, string>;
// Product.hasReceipt, ProductCollection.WithID and
// IStoreController.InitiatePurchase(string) exist. One mismatch fixed: the
// PurchaseFailureDescription constructor takes a CartItem, so the old
// single-reason failure overload no longer builds one.
//
// This is the legacy IStoreListener path, which 5.x marks Obsolete (warning,
// not error) and still ships fixes for; the 5.x StoreController API is a
// rewrite for the same one product and can wait. The pragma keeps the
// console clean so a real error in this file is not lost in the noise.
//
// That legacy path is a shim over the 5.x services, and it leaves two of
// their events unsubscribed (Legacy/UnityPurchasing.cs, 5.4.3). The package
// says so out loud under DEBUG — "IStoreService.Connect called without a
// callback defined for IStoreService.OnStoreConnected", the same for
// OnStoreDisconnected, and one for IPurchaseService.OnPurchasesFetchFailed.
// The third one is not cosmetic: the purchases fetch is what puts the
// receipt on the Product, and the shim starts it and then calls
// OnInitialized without waiting for it. Firing ready from OnInitialized
// therefore reported ownership one step too early — always false on a cold
// start — and nothing fired again afterwards, so an owner kept the NO ADS
// chip for the whole session. Ready now comes from the fetch itself, and
// the events are subscribed directly on the 5.x services (UnityIAPServices,
// which is not Obsolete) rather than through the shim.
//
// Gated by GRIDINFECT_IAP, which the Services asmdef defines automatically
// when com.unity.purchasing is in the manifest (a versionDefine, so there is
// no symbol to remember).
//
// Ownership is read from the store's own receipt, never from a local flag: a
// bool in PlayerPrefs is a one-line "unlock everything" for anyone with a file
// browser, and remove-ads is the only thing in this game worth forging.

#if GRIDINFECT_IAP
#pragma warning disable CS0618   // the legacy IAP API, on purpose; see the header
using System;
using UnityEngine;
using UnityEngine.Purchasing;

namespace GridInfect.Services
{
    public sealed class UnityIapPurchaseService : IDetailedStoreListener, IPurchaseService
    {
        public const string RemoveAdsProductId = "remove_ads";

        IStoreController _controller;
        IExtensionProvider _extensions;
        Action _ready;
        Action<bool> _pending;
        bool _started;
        bool _connected;

        public void Initialize(Action ready)
        {
            if (_started) return;
            _started = true;
            _ready = ready;

            // StandardPurchasingModule.Instance() is what settles which store
            // is the default one, and the service handles below are cached per
            // store name. Build the module first or they resolve against the
            // wrong store.
            var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
            builder.AddProduct(RemoveAdsProductId, ProductType.NonConsumable);

            // Before Initialize, because Initialize calls Connect() on this
            // same service and the warning is raised at the call.
            var store = UnityIAPServices.DefaultStore();
            store.OnStoreConnected += OnStoreConnected;
            store.OnStoreDisconnected += OnStoreDisconnected;

            UnityPurchasing.Initialize(this, builder);

            // After Initialize, and the order is load-bearing. Initialize
            // constructs the legacy PurchasingManager, whose own
            // OnPurchasesFetched handler is what copies each order's receipt
            // onto its Product. Both handlers hang off the same multicast
            // delegate and run in subscription order, so subscribing first
            // would read RemoveAdsOwned before any receipt exists.
            var purchases = UnityIAPServices.DefaultPurchase();
            purchases.OnPurchasesFetched += OnPurchasesFetched;
            purchases.OnPurchasesFetchFailed += OnPurchasesFetchFailed;
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
            if (_controller == null || !_connected) { owned?.Invoke(false); return; }
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

        // The store answered, but the receipts have not landed yet: the shim
        // starts the purchases fetch and calls this without waiting on it.
        // Ready belongs to OnPurchasesFetched, not here. Reaching this point
        // does mean the connection is up, which is what _connected tracks
        // from here on.
        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            _controller = controller;
            _extensions = extensions;
            _connected = true;
        }

        public void OnInitializeFailed(InitializationFailureReason error) => OnInitializeFailed(error, null);

        public void OnInitializeFailed(InitializationFailureReason error, string message)
        {
            // No store, no purchase, and the game plays on. The NO ADS chip
            // stays visible and its tap is inert, which is honest: the store
            // is what is unavailable, not the product. Ready fires here
            // because no purchases fetch will follow a failed initialize.
            _ready?.Invoke();
        }

        // --- 5.x service events ----------------------------------------------

        void OnStoreConnected() => _connected = true;

        // Billing can drop mid-session: a Play Store self-update, a killed
        // service, a lost network. Nothing to undo — RemoveAdsOwned still
        // reads the receipt already fetched — but a purchase started now
        // would sit there with no callback, so BuyRemoveAds refuses instead.
        void OnStoreDisconnected(StoreConnectionFailureDescription failure) => _connected = false;

        // The receipts are on their Products by now (see the ordering note in
        // Initialize), so this is the first moment RemoveAdsOwned is the
        // store's word rather than the default.
        void OnPurchasesFetched(Orders orders) => _ready?.Invoke();

        // Ownership is unknown and stays false. Firing anyway keeps the one
        // promise the callback makes: it always arrives.
        void OnPurchasesFetchFailed(PurchasesFetchFailureDescription failure) => _ready?.Invoke();

        // A completed purchase. On Google Play an owned non-consumable can
        // also arrive here at initialize, with nothing pending; the receipt
        // is what RemoveAdsOwned reads, so that case needs no work.
        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
        {
            bool isRemoveAds = string.Equals(
                args.purchasedProduct.definition.id, RemoveAdsProductId, StringComparison.Ordinal);
            var pending = _pending;
            _pending = null;
            pending?.Invoke(isRemoveAds);
            return PurchaseProcessingResult.Complete;
        }

        // Both overloads land here: the store calls one or the other, and
        // the reason is not something this game acts on.
        public void OnPurchaseFailed(Product product, PurchaseFailureReason reason) => Fail();

        public void OnPurchaseFailed(Product product, PurchaseFailureDescription description) => Fail();

        void Fail()
        {
            var pending = _pending;
            _pending = null;
            pending?.Invoke(false);
        }
    }
}
#endif

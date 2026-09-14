// R-701/R-702: the single non-consumable, over Unity IAP.
//
// Checked against the com.unity.purchasing 5.4.3 source (needle-mirror,
// 2026-09-14), not yet compiled in the editor. What was checked: every type
// here is in the UnityEngine.Purchasing namespace and the Unity.Purchasing
// assembly (the asmdef reference); IStoreListener's two OnInitializeFailed
// overloads; IAppleExtensions.RestoreTransactions takes Action<bool, string>;
// Product.uSku is definition.id; Order.CartOrdered.Items() and Order.Info
// (IOrderInfo); Orders.ConfirmedOrders/PendingOrders/DeferredOrders;
// ProductFetchFailed and the IProductService/IStoreService/IPurchaseService
// event signatures. One mismatch fixed earlier: the
// PurchaseFailureDescription constructor takes a CartItem, so the old
// single-reason failure overload no longer builds one.
//
// This is the legacy IStoreListener path, which 5.x marks Obsolete (warning,
// not error) and still ships fixes for; the 5.x StoreController API is a
// rewrite for the same one product and can wait. The pragma keeps the
// console clean so a real error in this file is not lost in the noise.
//
// That legacy path is a shim over the 5.x services, and it leaves several of
// their events unsubscribed, so they are subscribed here directly on
// UnityIAPServices (which is not Obsolete) rather than through the shim. The
// package says so out loud under DEBUG, and the third of those warnings is
// the one that mattered: "IPurchaseService.FetchPurchases called without a
// callback defined for IPurchaseService.OnPurchasesFetchFailed".
//
// Every subscription therefore goes up BEFORE UnityPurchasing.Initialize.
// Initialize is not the async call its shape suggests: it hands off to
// ConnectToStoreAndFetchProducts, which awaits Connect(), and a store that
// completes Connect inline then runs the whole chain -- products fetched,
// FetchPurchases, the fetch result -- on Initialize's own stack before it
// returns. That is exactly what the warning above was reporting, with
// Initialize still on the stack. A handler subscribed after Initialize is a
// handler subscribed to events that have already fired, which is why ready
// never arrived on those stores and an owner kept the NO ADS chip for the
// whole session.
//
// Ready comes from the purchases fetch, not from OnInitialized: the shim
// starts the fetch and calls OnInitialized without waiting on it, so
// ownership read there is always the default. It also comes from
// OnProductsFetchFailed, because a Connect that fails makes the products
// fetch throw store-not-connected, and on that reason the shim calls neither
// OnInitialized nor OnInitializeFailed -- ready would never arrive at all.
//
// Subscribing early costs one thing: our OnPurchasesFetched now runs before
// the legacy PurchasingManager's, and that handler is what copies each
// order's receipt onto its Product. So ownership is read from the Orders
// argument, which is the same store answer one step earlier.
//
// Gated by GRIDINFECT_IAP, which the Services asmdef defines automatically
// when com.unity.purchasing is in the manifest (a versionDefine, so there is
// no symbol to remember).
//
// Ownership is still the store's word, never a local flag: a bool in
// PlayerPrefs is a one-line "unlock everything" for anyone with a file
// browser, and remove-ads is the only thing in this game worth forging.
// _owned below is only ever assigned from a store response, is never
// persisted, and starts over as unanswered on every cold start.

#if GRIDINFECT_IAP
#pragma warning disable CS0618   // the legacy IAP API, on purpose; see the header
using System;
using System.Collections.Generic;
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
        Action<bool> _restore;
        bool? _owned;
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

            // All of it before Initialize; see the header. Initialize can run
            // the connect, the product fetch and the purchases fetch to
            // completion before it returns.
            var store = UnityIAPServices.DefaultStore();
            store.OnStoreConnected += OnStoreConnected;
            store.OnStoreDisconnected += OnStoreDisconnected;

            var products = UnityIAPServices.DefaultProduct();
            products.OnProductsFetchFailed += OnProductsFetchFailed;

            var purchases = UnityIAPServices.DefaultPurchase();
            purchases.OnPurchasesFetched += OnPurchasesFetched;
            purchases.OnPurchasesFetchFailed += OnPurchasesFetchFailed;

            UnityPurchasing.Initialize(this, builder);
        }

        // The store's last answer if it has given one, and the receipt on the
        // Product otherwise. The fallback covers the window before the first
        // fetch lands and the receipt the shim writes on a restore.
        public bool RemoveAdsOwned => _owned ?? HasRemoveAdsReceipt();

        bool HasRemoveAdsReceipt()
        {
            var product = _controller?.products?.WithID(RemoveAdsProductId);
            return product != null && product.hasReceipt;
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

            _extensions.GetExtension<IAppleExtensions>().RestoreTransactions((success, error) =>
            {
                if (!success) { owned?.Invoke(false); return; }

                // A successful restore only starts a purchases fetch -- the
                // receipts land with that, after this callback -- so reading
                // ownership here would answer with the pre-restore state.
                // Hand the answer to whichever fetch event arrives next.
                _restore += owned;
            });
        }

        // --- IStoreListener ---------------------------------------------------

        // The store answered, but the receipts have not landed yet: the shim
        // starts the purchases fetch and calls this without waiting on it.
        // Ready belongs to the fetch, not here. Reaching this point does mean
        // the connection is up, which is what _connected tracks from here on.
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
            Ready();
        }

        // --- 5.x service events ----------------------------------------------

        void OnStoreConnected() => _connected = true;

        // Billing can drop mid-session: a Play Store self-update, a killed
        // service, a lost network. Nothing to undo -- RemoveAdsOwned still
        // reads the answer already fetched -- but a purchase started now
        // would sit there with no callback, so BuyRemoveAds refuses instead.
        void OnStoreDisconnected(StoreConnectionFailureDescription failure) => _connected = false;

        // The products fetch failed, so no purchases fetch will follow. On the
        // one reason the shim recognises this is a second ready after its
        // OnInitializeFailed, which the interface allows; on every other
        // reason, store-not-connected included, it is the only ready there is.
        void OnProductsFetchFailed(ProductFetchFailed failure) => Ready();

        // The store's own purchase history, and the first moment ownership is
        // its word rather than the default. Read from the orders and not from
        // Product.hasReceipt: this runs before the legacy manager's handler,
        // which is what copies the receipts onto the Products, so they are
        // still bare here.
        void OnPurchasesFetched(Orders orders)
        {
            // Deferred orders are deliberately out: an Ask to Buy waiting on
            // a parent is not an entitlement yet. A later fetch, or the
            // purchase itself, answers again when it becomes one.
            _owned = HasRemoveAds(orders.ConfirmedOrders) || HasRemoveAds(orders.PendingOrders);
            Ready();
        }

        // Ownership is unknown and stays unanswered. Firing anyway keeps the
        // one promise the callback makes: it always arrives.
        void OnPurchasesFetchFailed(PurchasesFetchFailureDescription failure) => Ready();

        static bool HasRemoveAds(IEnumerable<Order> orders)
        {
            if (orders == null) return false;

            foreach (var order in orders)
            {
                var items = order?.CartOrdered?.Items();
                if (items == null) continue;

                foreach (var item in items)
                {
                    // uSku is the product definition id; a listing id can
                    // differ from it on a multi-listing product.
                    if (string.Equals(item?.Product?.uSku, RemoveAdsProductId, StringComparison.Ordinal))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        // Both callbacks that wait on the store's answer, in one place so
        // every path that ends the wait ends both of them.
        void Ready()
        {
            _ready?.Invoke();

            var restore = _restore;
            _restore = null;
            restore?.Invoke(RemoveAdsOwned);
        }

        // A completed purchase. On Google Play an owned non-consumable can
        // also arrive here at initialize, with nothing pending; the store is
        // still the one saying so, so it counts either way.
        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
        {
            bool isRemoveAds = string.Equals(
                args.purchasedProduct.definition.id, RemoveAdsProductId, StringComparison.Ordinal);
            if (isRemoveAds) _owned = true;
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

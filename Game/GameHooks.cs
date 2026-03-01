namespace RBN.Modlib.Game;

using BepInEx.Logging;
using RBN.Modlib.UI;
using UnityEngine.SceneManagement;

public static class GameHooks
{
    private static ManualLogSource? _logger;
    private static GameObject? _runtimeObject;

    public static bool IsInitialized { get; private set; }

    public static event Action<string>? SceneLoaded;
    public static event Action<float>? MoneyChanged;
    public static event Action? ItemSold;
    public static event Action<int>? ResearchTicketsChanged;
    public static event Action<bool>? PauseChanged;
    public static event Action<bool>? MenuStateChanged;
    public static event Action<bool>? InputBlockStateChanged;
    public static event Action<string>? ActiveSaveFileChanged;
    public static event Action<bool>? SaveLoadStateChanged;

    public static event Action<string>? ShopItemUnlocked;
    public static event Action<string>? ResearchItemResearched;
    public static event Action<bool>? InventoryOpenStateChanged;
    public static event Action<string>? ActiveToolChanged;
    public static event Action<string>? ActiveContractChanged;
    public static event Action<string>? OreLimitStateChanged;
    public static event Action<int>? ActiveQuestCountChanged;
    public static event Action<int>? AvailableQuestCountChanged;
    public static event Action<string>? QuestActivated;
    public static event Action<string>? QuestPaused;
    public static event Action<string>? QuestCompleted;
    public static event Action<int>? ShopPurchaseTotalChanged;
    public static event Action? WorldOreCleared;

    public static void Initialize(ManualLogSource? logger = null)
    {
        if (IsInitialized) return;

        _logger = logger;
        _runtimeObject = new GameObject("[RBN.Modlib.GameHooks]");
        _runtimeObject.hideFlags = HideFlags.HideAndDontSave;
        UnityEngine.Object.DontDestroyOnLoad(_runtimeObject);
        _runtimeObject.AddComponent<HookRuntime>();
        IsInitialized = true;
    }

    public static void Shutdown()
    {
        if (!IsInitialized) return;

        IsInitialized = false;
        if (_runtimeObject != null)
        {
            UnityEngine.Object.Destroy(_runtimeObject);
            _runtimeObject = null;
        }

        SceneLoaded = null;
        MoneyChanged = null;
        ItemSold = null;
        ResearchTicketsChanged = null;
        PauseChanged = null;
        MenuStateChanged = null;
        InputBlockStateChanged = null;
        ActiveSaveFileChanged = null;
        SaveLoadStateChanged = null;

        ShopItemUnlocked = null;
        ResearchItemResearched = null;
        InventoryOpenStateChanged = null;
        ActiveToolChanged = null;
        ActiveContractChanged = null;
        OreLimitStateChanged = null;
        ActiveQuestCountChanged = null;
        AvailableQuestCountChanged = null;
        QuestActivated = null;
        QuestPaused = null;
        QuestCompleted = null;
        ShopPurchaseTotalChanged = null;
        WorldOreCleared = null;
    }

    private static void InvokeSafe(Action? handlers, string eventName)
    {
        if (handlers == null) return;

        foreach (var entry in handlers.GetInvocationList())
        {
            try
            {
                ((Action)entry).Invoke();
            }
            catch (Exception ex)
            {
                _logger?.LogWarning($"GameHooks.{eventName} subscriber threw: {ex.Message}");
            }
        }
    }

    private static void InvokeSafe<T>(Action<T>? handlers, T arg, string eventName)
    {
        if (handlers == null) return;

        foreach (var entry in handlers.GetInvocationList())
        {
            try
            {
                ((Action<T>)entry).Invoke(arg);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning($"GameHooks.{eventName} subscriber threw: {ex.Message}");
            }
        }
    }

    private sealed class HookRuntime : MonoBehaviour
    {
        private static readonly BindingFlags AnyInstance =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        private IBoundSignal? _moneyUpdatedSubscription;
        private IBoundSignal? _itemSoldSubscription;
        private IBoundSignal? _shopItemUnlockedSubscription;
        private IBoundSignal? _researchTicketsSubscription;
        private IBoundSignal? _researchItemResearchedSubscription;
        private IBoundSignal? _gamePausedSubscription;
        private IBoundSignal? _gameUnpausedSubscription;
        private IBoundSignal? _inventoryOpenedSubscription;
        private IBoundSignal? _inventoryClosedSubscription;
        private IBoundSignal? _clearOreSubscription;
        private IBoundSignal? _contractsUpdatedSubscription;

        private float _nextBindAttemptTime;
        private float _nextPollTime;

        private bool _hasMenuState;
        private bool _lastMenuState;
        private bool _hasInputBlockState;
        private bool _lastInputBlockState;
        private bool _hasInventoryOpenState;
        private bool _lastInventoryOpenState;
        private bool _hasSaveNameState;
        private string _lastSaveName = string.Empty;
        private bool _hasLoadingState;
        private bool _lastLoadingState;
        private bool _hasOreState;
        private string _lastOreState = string.Empty;
        private bool _hasContractLabel;
        private string _lastContractLabel = string.Empty;
        private bool _hasToolLabel;
        private string _lastToolLabel = string.Empty;
        private bool _hasActiveQuestCount;
        private int _lastActiveQuestCount;
        private bool _hasAvailableQuestCount;
        private int _lastAvailableQuestCount;
        private bool _hasShopPurchaseTotal;
        private int _lastShopPurchaseTotal;

        private readonly HashSet<int> _lastActiveQuestIds = new();
        private readonly HashSet<int> _lastCompletedQuestIds = new();

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            _nextBindAttemptTime = 0f;
            _nextPollTime = 0f;
        }

        private void Start()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.IsValid() && !string.IsNullOrWhiteSpace(scene.name))
            {
                InvokeSafe(SceneLoaded, scene.name, nameof(SceneLoaded));
            }

            PollAll(force: true);
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            DisposeSubscriptions();
        }

        private void Update()
        {
            if (Time.unscaledTime >= _nextBindAttemptTime)
            {
                _nextBindAttemptTime = Time.unscaledTime + 1f;
                TryBindSignals();
            }

            if (Time.unscaledTime >= _nextPollTime)
            {
                _nextPollTime = Time.unscaledTime + 0.2f;
                PollAll(force: false);
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            DisposeSubscriptions();
            _nextBindAttemptTime = 0f;
            _nextPollTime = 0f;
            InvokeSafe(SceneLoaded, scene.name, nameof(SceneLoaded));
            PollAll(force: true);
        }

        private void PollAll(bool force)
        {
            PollUiState(force);
            PollSaveState(force);
            PollOreState(force);
            PollContractState(force);
            PollInventoryState(force);
            PollQuestState(force);
            PollShopState(force);
        }

        private void TryBindSignals()
        {
            TryBindEconomySignals();
            TryBindResearchSignals();
            TryBindPauseSignals();
            TryBindInventorySignals();
            TryBindDebugSignals();
            TryBindContractSignals();
        }

        private void TryBindEconomySignals()
        {
            var instance = GameReflection.TryGetSingletonInstance("EconomyManager");
            BindOrRebind(ref _moneyUpdatedSubscription, instance, "OnMoneyUpdated", nameof(OnMoneyUpdated));
            BindOrRebind(ref _itemSoldSubscription, instance, "ItemSold", nameof(OnItemSold));
            BindOrRebind(ref _shopItemUnlockedSubscription, instance, "ShopItemUnlocked", nameof(OnShopItemUnlocked));
        }

        private void TryBindResearchSignals()
        {
            var instance = GameReflection.TryGetSingletonInstance("ResearchManager");
            BindOrRebind(ref _researchTicketsSubscription, instance, "ResearchTicketsUpdated", nameof(OnResearchTicketsUpdated));
            BindOrRebind(ref _researchItemResearchedSubscription, instance, "ResearchItemResearched", nameof(OnResearchItemResearched));
        }

        private void TryBindPauseSignals()
        {
            var instance = GameReflection.TryGetSingletonInstance("GameManager");
            BindOrRebind(ref _gamePausedSubscription, instance, "GamePaused", nameof(OnGamePaused));
            BindOrRebind(ref _gameUnpausedSubscription, instance, "GameUnpaused", nameof(OnGameUnpaused));
        }

        private void TryBindInventorySignals()
        {
            var instance = GameReflection.TryGetSingletonInstance("InventoryUIManager");
            BindOrRebind(ref _inventoryOpenedSubscription, instance, "InventoryOpened", nameof(OnInventoryOpened));
            BindOrRebind(ref _inventoryClosedSubscription, instance, "InventoryClosed", nameof(OnInventoryClosed));
        }

        private void TryBindDebugSignals()
        {
            var instance = GameReflection.TryGetSingletonInstance("DebugManager");
            BindOrRebind(ref _clearOreSubscription, instance, "ClearedAllPhysicsOrePieces", nameof(OnClearedAllPhysicsOrePieces));
        }

        private void TryBindContractSignals()
        {
            var instance = GameReflection.TryGetSingletonInstance("ContractsManager");
            BindOrRebind(ref _contractsUpdatedSubscription, instance, "OnActiveContractUpdated", nameof(OnContractChanged));
        }

        private void PollUiState(bool force)
        {
            var uiManager = GameReflection.TryGetSingletonInstance("UIManager");
            var inAnyMenu = TryInvokeBool(uiManager, "IsInAnyMenu");
            if (force || !_hasMenuState || inAnyMenu != _lastMenuState)
            {
                _hasMenuState = true;
                _lastMenuState = inAnyMenu;
                InvokeSafe(MenuStateChanged, inAnyMenu, nameof(MenuStateChanged));
            }

            var isInputBlocked = UiControlUtils.ShouldHidePopupUi();
            if (force || !_hasInputBlockState || isInputBlocked != _lastInputBlockState)
            {
                _hasInputBlockState = true;
                _lastInputBlockState = isInputBlocked;
                InvokeSafe(InputBlockStateChanged, isInputBlocked, nameof(InputBlockStateChanged));
            }

            var inventoryManager = GameReflection.TryGetSingletonInstance("InventoryUIManager");
            var inventoryOpen = TryInvokeBool(inventoryManager, "IsShowingInventory");
            if (force || !_hasInventoryOpenState || inventoryOpen != _lastInventoryOpenState)
            {
                _hasInventoryOpenState = true;
                _lastInventoryOpenState = inventoryOpen;
                InvokeSafe(InventoryOpenStateChanged, inventoryOpen, nameof(InventoryOpenStateChanged));
            }
        }

        private void PollSaveState(bool force)
        {
            var saveManager = GameReflection.TryGetSingletonInstance("SavingLoadingManager");
            if (saveManager == null) return;

            var activeSaveName = TryReadStringMember(saveManager, "ActiveSaveFileName");
            if (!string.IsNullOrWhiteSpace(activeSaveName))
            {
                var trimmedSaveName = activeSaveName!.Trim();
                if (force || !_hasSaveNameState || !string.Equals(trimmedSaveName, _lastSaveName, StringComparison.Ordinal))
                {
                    _hasSaveNameState = true;
                    _lastSaveName = trimmedSaveName;
                    InvokeSafe(ActiveSaveFileChanged, trimmedSaveName, nameof(ActiveSaveFileChanged));
                }
            }

            var isLoading = TryReadBoolMember(saveManager, "IsCurrentlyLoadingGame");
            if (isLoading.HasValue && (force || !_hasLoadingState || isLoading.Value != _lastLoadingState))
            {
                _hasLoadingState = true;
                _lastLoadingState = isLoading.Value;
                InvokeSafe(SaveLoadStateChanged, isLoading.Value, nameof(SaveLoadStateChanged));
            }
        }

        private void PollOreState(bool force)
        {
            var oreLimitManager = GameReflection.TryGetSingletonInstance("OreLimitManager");
            if (oreLimitManager == null) return;

            var state = TryReadMember(oreLimitManager, "OreLimitState")?.ToString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(state)) return;

            if (force || !_hasOreState || !string.Equals(state, _lastOreState, StringComparison.Ordinal))
            {
                _hasOreState = true;
                _lastOreState = state;
                InvokeSafe(OreLimitStateChanged, state, nameof(OreLimitStateChanged));
            }
        }

        private void PollContractState(bool force)
        {
            var contractsManager = GameReflection.TryGetSingletonInstance("ContractsManager");
            if (contractsManager == null) return;

            var contract = TryReadMember(contractsManager, "ActiveContract");
            var label = contract == null ? string.Empty : BuildContractLabel(contract);
            if (force || !_hasContractLabel || !string.Equals(label, _lastContractLabel, StringComparison.Ordinal))
            {
                _hasContractLabel = true;
                _lastContractLabel = label;
                InvokeSafe(ActiveContractChanged, label, nameof(ActiveContractChanged));
            }
        }

        private void PollInventoryState(bool force)
        {
            var playerController = GameReflection.TryGetSingletonInstance("PlayerController");
            var inventory = playerController != null
                ? TryReadMember(playerController, "Inventory")
                : GameReflection.TryGetSingletonInstance("PlayerInventory");
            if (inventory == null) return;

            var activeTool = TryReadMember(inventory, "ActiveTool");
            var toolLabel = BuildToolLabel(activeTool);
            if (force || !_hasToolLabel || !string.Equals(toolLabel, _lastToolLabel, StringComparison.Ordinal))
            {
                _hasToolLabel = true;
                _lastToolLabel = toolLabel;
                InvokeSafe(ActiveToolChanged, toolLabel, nameof(ActiveToolChanged));
            }
        }

        private void PollQuestState(bool force)
        {
            var questManager = GameReflection.TryGetSingletonInstance("QuestManager");
            if (questManager == null) return;

            var activeIds = ReadIdSet(questManager, "GetActiveQuestIDs");
            var completedIds = ReadIdSet(questManager, "GetCompletedQuestIDs");

            if (force)
            {
                _lastActiveQuestIds.Clear();
                _lastCompletedQuestIds.Clear();
                foreach (var id in activeIds) _lastActiveQuestIds.Add(id);
                foreach (var id in completedIds) _lastCompletedQuestIds.Add(id);
            }
            else
            {
                foreach (var id in activeIds)
                {
                    if (_lastActiveQuestIds.Contains(id)) continue;
                    InvokeSafe(QuestActivated, BuildQuestLabel(questManager, id), nameof(QuestActivated));
                }

                foreach (var id in _lastActiveQuestIds)
                {
                    if (activeIds.Contains(id)) continue;
                    if (completedIds.Contains(id)) continue;
                    InvokeSafe(QuestPaused, BuildQuestLabel(questManager, id), nameof(QuestPaused));
                }

                foreach (var id in completedIds)
                {
                    if (_lastCompletedQuestIds.Contains(id)) continue;
                    InvokeSafe(QuestCompleted, BuildQuestLabel(questManager, id), nameof(QuestCompleted));
                }

                _lastActiveQuestIds.Clear();
                _lastCompletedQuestIds.Clear();
                foreach (var id in activeIds) _lastActiveQuestIds.Add(id);
                foreach (var id in completedIds) _lastCompletedQuestIds.Add(id);
            }

            var activeCount = activeIds.Count;
            if (force || !_hasActiveQuestCount || activeCount != _lastActiveQuestCount)
            {
                _hasActiveQuestCount = true;
                _lastActiveQuestCount = activeCount;
                InvokeSafe(ActiveQuestCountChanged, activeCount, nameof(ActiveQuestCountChanged));
            }

            var availableCount = CountEnumerable(TryInvoke(questManager, "GetAvailableQuests"));
            if (force || !_hasAvailableQuestCount || availableCount != _lastAvailableQuestCount)
            {
                _hasAvailableQuestCount = true;
                _lastAvailableQuestCount = availableCount;
                InvokeSafe(AvailableQuestCountChanged, availableCount, nameof(AvailableQuestCountChanged));
            }
        }

        private void PollShopState(bool force)
        {
            var economy = GameReflection.TryGetSingletonInstance("EconomyManager");
            if (economy == null) return;

            var totalPurchased = ReadShopPurchaseTotal(economy);
            if (force || !_hasShopPurchaseTotal || totalPurchased != _lastShopPurchaseTotal)
            {
                _hasShopPurchaseTotal = true;
                _lastShopPurchaseTotal = totalPurchased;
                InvokeSafe(ShopPurchaseTotalChanged, totalPurchased, nameof(ShopPurchaseTotalChanged));
            }
        }

        private void OnMoneyUpdated(float amount)
        {
            InvokeSafe(MoneyChanged, amount, nameof(MoneyChanged));
        }

        private void OnItemSold()
        {
            InvokeSafe(ItemSold, nameof(ItemSold));
        }

        private void OnShopItemUnlocked(object? shopItem)
        {
            InvokeSafe(ShopItemUnlocked, BuildShopItemLabel(shopItem), nameof(ShopItemUnlocked));
        }

        private void OnResearchTicketsUpdated(int amount)
        {
            InvokeSafe(ResearchTicketsChanged, amount, nameof(ResearchTicketsChanged));
        }

        private void OnResearchItemResearched(object? researchedItem)
        {
            InvokeSafe(ResearchItemResearched, BuildResearchItemLabel(researchedItem), nameof(ResearchItemResearched));
        }

        private void OnGamePaused()
        {
            InvokeSafe(PauseChanged, true, nameof(PauseChanged));
        }

        private void OnGameUnpaused()
        {
            InvokeSafe(PauseChanged, false, nameof(PauseChanged));
        }

        private void OnInventoryOpened()
        {
            _hasInventoryOpenState = true;
            _lastInventoryOpenState = true;
            InvokeSafe(InventoryOpenStateChanged, true, nameof(InventoryOpenStateChanged));
        }

        private void OnInventoryClosed()
        {
            _hasInventoryOpenState = true;
            _lastInventoryOpenState = false;
            InvokeSafe(InventoryOpenStateChanged, false, nameof(InventoryOpenStateChanged));
        }

        private void OnClearedAllPhysicsOrePieces()
        {
            InvokeSafe(WorldOreCleared, nameof(WorldOreCleared));
        }

        private void OnContractChanged()
        {
            PollContractState(force: true);
        }

        private void BindOrRebind(ref IBoundSignal? subscription, object? source, string signalName, string handlerMethodName)
        {
            if (source == null)
            {
                subscription?.Dispose();
                subscription = null;
                return;
            }

            if (subscription != null &&
                ReferenceEquals(subscription.Source, source) &&
                string.Equals(subscription.SignalName, signalName, StringComparison.Ordinal))
            {
                return;
            }

            subscription?.Dispose();
            subscription = null;

            var handlerMethod = GetType().GetMethod(handlerMethodName, AnyInstance);
            if (handlerMethod == null) return;

            if (TryCreateEventSubscription(source, signalName, handlerMethod, this, out var eventSubscription))
            {
                subscription = eventSubscription;
                return;
            }

            if (TryCreateDelegateFieldSubscription(source, signalName, handlerMethod, out var fieldSubscription))
            {
                subscription = fieldSubscription;
            }
        }

        private static bool TryCreateEventSubscription(object source, string eventName, MethodInfo handlerMethod, HookRuntime target, out IBoundSignal? subscription)
        {
            subscription = null;
            var eventInfo = source.GetType().GetEvent(eventName, AnyInstance);
            if (eventInfo == null || eventInfo.EventHandlerType == null) return false;

            Delegate? handler;
            try
            {
                handler = Delegate.CreateDelegate(eventInfo.EventHandlerType, target, handlerMethod, throwOnBindFailure: false);
            }
            catch
            {
                handler = null;
            }

            if (handler == null) return false;

            try
            {
                eventInfo.AddEventHandler(source, handler);
                subscription = new EventSignalSubscription(source, eventName, eventInfo, handler);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool TryCreateDelegateFieldSubscription(object source, string fieldName, MethodInfo handlerMethod, out IBoundSignal? subscription)
        {
            subscription = null;
            var field = source.GetType().GetField(fieldName, AnyInstance);
            if (field == null || !typeof(Delegate).IsAssignableFrom(field.FieldType)) return false;

            Delegate? handler;
            try
            {
                handler = Delegate.CreateDelegate(field.FieldType, this, handlerMethod, throwOnBindFailure: false);
            }
            catch
            {
                handler = null;
            }

            if (handler == null) return false;

            try
            {
                var current = field.GetValue(source) as Delegate;
                var combined = Delegate.Combine(current, handler);
                field.SetValue(source, combined);
                subscription = new DelegateFieldSignalSubscription(source, fieldName, field, handler);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void DisposeSubscriptions()
        {
            _moneyUpdatedSubscription?.Dispose();
            _moneyUpdatedSubscription = null;
            _itemSoldSubscription?.Dispose();
            _itemSoldSubscription = null;
            _shopItemUnlockedSubscription?.Dispose();
            _shopItemUnlockedSubscription = null;
            _researchTicketsSubscription?.Dispose();
            _researchTicketsSubscription = null;
            _researchItemResearchedSubscription?.Dispose();
            _researchItemResearchedSubscription = null;
            _gamePausedSubscription?.Dispose();
            _gamePausedSubscription = null;
            _gameUnpausedSubscription?.Dispose();
            _gameUnpausedSubscription = null;
            _inventoryOpenedSubscription?.Dispose();
            _inventoryOpenedSubscription = null;
            _inventoryClosedSubscription?.Dispose();
            _inventoryClosedSubscription = null;
            _clearOreSubscription?.Dispose();
            _clearOreSubscription = null;
            _contractsUpdatedSubscription?.Dispose();
            _contractsUpdatedSubscription = null;
        }

        private static object? TryReadMember(object instance, string memberName)
        {
            var type = instance.GetType();

            var property = type.GetProperty(memberName, AnyInstance);
            if (property != null && property.GetIndexParameters().Length == 0)
            {
                try
                {
                    return property.GetValue(instance, null);
                }
                catch
                {
                    // ignored
                }
            }

            var field = type.GetField(memberName, AnyInstance);
            if (field != null)
            {
                try
                {
                    return field.GetValue(instance);
                }
                catch
                {
                    // ignored
                }
            }

            return null;
        }

        private static string? TryReadStringMember(object instance, string memberName)
        {
            return TryReadMember(instance, memberName) as string;
        }

        private static bool? TryReadBoolMember(object instance, string memberName)
        {
            var value = TryReadMember(instance, memberName);
            return value is bool b ? b : null;
        }

        private static object? TryInvoke(object? instance, string methodName, params object[] args)
        {
            if (instance == null) return null;

            var type = instance.GetType();
            var methods = type.GetMethods(AnyInstance);
            for (var i = 0; i < methods.Length; i++)
            {
                var method = methods[i];
                if (!string.Equals(method.Name, methodName, StringComparison.Ordinal)) continue;

                var parameters = method.GetParameters();
                if (parameters.Length != args.Length) continue;

                try
                {
                    return method.Invoke(instance, args);
                }
                catch
                {
                    return null;
                }
            }

            return null;
        }

        private static bool TryInvokeBool(object? instance, string methodName)
        {
            return TryInvoke(instance, methodName) is bool value && value;
        }

        private static string TryInvokeString(object? instance, string methodName, string fallback = "")
        {
            var value = TryInvoke(instance, methodName);
            return value?.ToString() ?? fallback;
        }

        private static HashSet<int> ReadIdSet(object source, string methodName)
        {
            var result = new HashSet<int>();
            var values = TryInvoke(source, methodName) as System.Collections.IEnumerable;
            if (values == null) return result;

            foreach (var item in values)
            {
                if (item == null) continue;
                try
                {
                    result.Add(Convert.ToInt32(item, System.Globalization.CultureInfo.InvariantCulture));
                }
                catch
                {
                    // ignored
                }
            }

            return result;
        }

        private static object? GetQuestById(object questManager, int questId)
        {
            var method = questManager.GetType().GetMethod("GetQuestByID", AnyInstance);
            if (method == null) return null;

            var parameters = method.GetParameters();
            if (parameters.Length != 1) return null;

            try
            {
                var argType = parameters[0].ParameterType;
                object arg = argType.IsEnum
                    ? Enum.ToObject(argType, questId)
                    : Convert.ChangeType(questId, argType, System.Globalization.CultureInfo.InvariantCulture)!;
                return method.Invoke(questManager, new[] { arg });
            }
            catch
            {
                return null;
            }
        }

        private static string BuildQuestLabel(object questManager, int questId)
        {
            var quest = GetQuestById(questManager, questId);
            if (quest == null)
            {
                return questId.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }

            var questIdName = TryReadMember(quest, "QuestID")?.ToString() ?? questId.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var questName = TryReadStringMember(quest, "Name");
            if (string.IsNullOrWhiteSpace(questName))
            {
                return questIdName;
            }

            return questIdName + ": " + questName!.Trim();
        }

        private static string BuildShopItemLabel(object? shopItem)
        {
            if (shopItem == null) return string.Empty;

            var name = TryInvokeString(shopItem, "GetName");
            var savable = TryInvoke(shopItem, "GetSavableObjectID")?.ToString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(name)) return savable;
            if (string.IsNullOrWhiteSpace(savable)) return name;
            return savable + ": " + name;
        }

        private static string BuildResearchItemLabel(object? researchItem)
        {
            if (researchItem == null) return string.Empty;

            var name = TryInvokeString(researchItem, "GetName");
            var savable = TryInvoke(researchItem, "GetSavableObjectID")?.ToString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(name)) return savable;
            if (string.IsNullOrWhiteSpace(savable)) return name;
            return savable + ": " + name;
        }

        private static string BuildToolLabel(object? activeTool)
        {
            if (activeTool == null) return string.Empty;

            var name = TryReadStringMember(activeTool, "Name");
            if (string.IsNullOrWhiteSpace(name))
            {
                name = activeTool.GetType().Name;
            }

            var quantity = TryReadMember(activeTool, "Quantity");
            var qty = quantity != null ? Convert.ToString(quantity, System.Globalization.CultureInfo.InvariantCulture) : string.Empty;
            if (string.IsNullOrWhiteSpace(qty) || qty == "1") return name!;
            return name + " x" + qty;
        }

        private static string BuildContractLabel(object contract)
        {
            var name = TryReadStringMember(contract, "Name") ?? string.Empty;
            var percent = TryInvokeString(contract, "GetPercentCompleteText", string.Empty);
            if (string.IsNullOrWhiteSpace(percent)) return name;
            if (string.IsNullOrWhiteSpace(name)) return percent;
            return name + " (" + percent + ")";
        }

        private static int ReadShopPurchaseTotal(object economy)
        {
            var purchases = TryReadMember(economy, "ShopPurchases");
            if (purchases == null) return 0;

            var entries = TryReadMember(purchases, "Purchases") as System.Collections.IEnumerable;
            if (entries == null) return 0;

            var total = 0;
            foreach (var entry in entries)
            {
                if (entry == null) continue;
                var amountObj = TryReadMember(entry, "AmountPurchased");
                if (amountObj == null) continue;

                try
                {
                    total += Convert.ToInt32(amountObj, System.Globalization.CultureInfo.InvariantCulture);
                }
                catch
                {
                    // ignored
                }
            }

            return total;
        }

        private static int CountEnumerable(object? sequenceObj)
        {
            if (sequenceObj is not System.Collections.IEnumerable sequence) return 0;

            var count = 0;
            foreach (var _ in sequence)
            {
                count++;
            }

            return count;
        }
    }

    private interface IBoundSignal : IDisposable
    {
        object Source { get; }
        string SignalName { get; }
    }

    private sealed class EventSignalSubscription : IBoundSignal
    {
        private readonly EventInfo _eventInfo;
        private readonly Delegate _handler;

        public EventSignalSubscription(object source, string signalName, EventInfo eventInfo, Delegate handler)
        {
            Source = source;
            SignalName = signalName;
            _eventInfo = eventInfo;
            _handler = handler;
        }

        public object Source { get; }
        public string SignalName { get; }

        public void Dispose()
        {
            try
            {
                _eventInfo.RemoveEventHandler(Source, _handler);
            }
            catch
            {
                // Best effort cleanup.
            }
        }
    }

    private sealed class DelegateFieldSignalSubscription : IBoundSignal
    {
        private readonly FieldInfo _fieldInfo;
        private readonly Delegate _handler;

        public DelegateFieldSignalSubscription(object source, string signalName, FieldInfo fieldInfo, Delegate handler)
        {
            Source = source;
            SignalName = signalName;
            _fieldInfo = fieldInfo;
            _handler = handler;
        }

        public object Source { get; }
        public string SignalName { get; }

        public void Dispose()
        {
            try
            {
                var current = _fieldInfo.GetValue(Source) as Delegate;
                var removed = Delegate.Remove(current, _handler);
                _fieldInfo.SetValue(Source, removed);
            }
            catch
            {
                // Best effort cleanup.
            }
        }
    }
}

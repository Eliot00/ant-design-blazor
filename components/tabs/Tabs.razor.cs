﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AntDesign.JsInterop;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace AntDesign
{
    public partial class Tabs : AntDomComponentBase
    {
        private const string PrefixCls = "ant-tabs";
        private bool IsHorizontal { get => TabPosition == AntDesign.TabPosition.Top || TabPosition == AntDesign.TabPosition.Bottom; }

        //private ClassMapper _barClassMapper = new ClassMapper();
        //private ClassMapper _prevClassMapper = new ClassMapper();
        //private ClassMapper _nextClassMapper = new ClassMapper();
        //private ClassMapper _navClassMapper = new ClassMapper();
        private TabPane _activePane;

        private TabPane _renderedActivePane;

        private ElementReference _scrollTabBar;
        private ElementReference _tabBars;

        private string _inkStyle;

        private string _navStyle;

        //private string _contentStyle;
        //private bool? _prevIconEnabled;
        //private bool? _nextIconEnabled;
        private string _operationClass;

        private string _tabsNavWarpPingClass;
        private string _operationStyle;

        private int _navIndex;
        private int _scrollOffset;
        private int _navTotal;
        private int _navSection;
        private bool _needRefresh;
        private bool _afterFirstRender;
        private bool _activePaneChanged;

        internal List<TabPane> _panes = new List<TabPane>();

        #region Parameters

        [Parameter]
        public RenderFragment ChildContent { get; set; }

        private string _activeKey;

        /// <summary>
        /// Current <see cref="TabPane"/>'s <see cref="TabPane.Key"/>
        /// </summary>
        [Parameter]
        public string ActiveKey
        {
            get
            {
                return _activeKey;
            }
            set
            {
                if (_activeKey != value)
                {
                    _activeKey = value;

                    if (_panes.Count == 0)
                        return;

                    var tabPane = _panes.Find(p => p.Key == value);

                    if (tabPane == null)
                        return;

                    ActivatePane(tabPane);
                }
            }
        }

        [Parameter]
        public EventCallback<string> ActiveKeyChanged { get; set; }

        /// <summary>
        /// Whether to change tabs with animation. Only works while <see cref="TabPosition"/> = <see cref="TabPosition.Top"/> or <see cref="TabPosition.Bottom"/>
        /// </summary>
        [Parameter]
        public bool Animated { get; set; } = true;

        /// <summary>
        /// Replace the TabBar
        /// </summary>
        [Parameter]
        public object RenderTabBar { get; set; }

        /// <summary>
        /// Initial active <see cref="TabPane"/>'s <see cref="TabPane.Key"/>, if <see cref="ActiveKey"/> is not set
        /// </summary>
        [Parameter]
        public string DefaultActiveKey { get; set; }

        /// <summary>
        /// Hide plus icon or not. Only works while <see cref="Type"/> = <see cref="TabType.EditableCard"/>
        /// </summary>
        [Parameter]
        public bool HideAdd { get; set; } = false;

        /// <summary>
        /// Preset tab bar size
        /// </summary>
        [Parameter]
        public string Size { get; set; } = TabSize.Default;

        /// <summary>
        /// Extra content in tab bar
        /// </summary>
        [Parameter]
        public RenderFragment TabBarExtraContent { get; set; }

        /// <summary>
        /// The gap between tabs
        /// </summary>
        [Parameter]
        public int TabBarGutter { get; set; }

        /// <summary>
        /// Tab bar style object
        /// </summary>
        [Parameter]
        public string TabBarStyle { get; set; }

        /// <summary>
        /// Position of tabs
        /// </summary>
        [Parameter]
        public string TabPosition { get; set; } = AntDesign.TabPosition.Top;

        /// <summary>
        /// Basic style of tabs
        /// </summary>
        [Parameter]
        public string Type { get; set; } = TabType.Line;

        /// <summary>
        /// Callback executed when active tab is changed
        /// </summary>
        [Parameter]
        public EventCallback<string> OnChange { get; set; }

        /// <summary>
        /// Callback executed when tab is added or removed. Only works while <see cref="Type"/> = <see cref="TabType.EditableCard"/>
        /// </summary>
        [Parameter]
        public Func<string, string, Task<bool>> OnEdit { get; set; } = (key, action) => Task.FromResult(true);

        [Parameter]
        public EventCallback OnAddClick { get; set; }

        [Parameter]
        public EventCallback<string> AfterTabCreated { get; set; }

        /// <summary>
        /// Callback executed when next button is clicked
        /// </summary>
        [Parameter]
        public EventCallback OnNextClick { get; set; }

        /// <summary>
        /// Callback executed when prev button is clicked
        /// </summary>
        [Parameter]
        public EventCallback OnPrevClick { get; set; }

        /// <summary>
        /// Callback executed when tab is clicked
        /// </summary>
        [Parameter]
        public EventCallback<string> OnTabClick { get; set; }

        /// <summary>
        /// Whether to turn on keyboard navigation
        /// </summary>
        [Parameter]
        public bool Keyboard { get; set; } = true;

        [Parameter]
        public bool Draggable { get; set; }

        [CascadingParameter]
        public Card Card { get; set; }

        #endregion Parameters

        protected override void OnInitialized()
        {
            base.OnInitialized();

            ClassMapper.Clear()
                .Add(PrefixCls)
                .Add($"{PrefixCls}-{TabPosition}")
                .Add($"{PrefixCls}-{Type}")
                .If($"{PrefixCls}-large", () => Size == TabSize.Large || Card != null)
                .If($"{PrefixCls}-head-tabs", () => Card != null)
                .If($"{PrefixCls}-small", () => Size == TabSize.Small)
                .GetIf(() => $"{PrefixCls}-{TabType.Card}", () => Type == TabType.EditableCard)
                .If($"{PrefixCls}-no-animation", () => !Animated);
        }

        public override Task SetParametersAsync(ParameterView parameters)
        {
            _needRefresh = true;
            _renderedActivePane = null;
            string type = parameters.GetValueOrDefault<string>(nameof(Type));
            if (type == TabType.Card)
            {
                // according to ant design documents,
                // Animated default to false when type="card"
                Animated = false;
            }

            string position = parameters.GetValueOrDefault<string>(nameof(TabPosition));
            if (!string.IsNullOrEmpty(position))
            {
                _navIndex = 0;
            }

            return base.SetParametersAsync(parameters);
        }

        protected override void OnParametersSet()
        {
            base.OnParametersSet();

            //if (Type == TabType.EditableCard && !HideAdd)
            //{
            //    TabBarExtraContent = (b) =>
            //    {
            //        b.OpenComponent<Icon>(0);
            //        b.AddAttribute(1, "Type", "plus");
            //        b.AddAttribute(2, "class", $"{PrefixCls}-new-tab");
            //        b.AddAttribute(3, "onclick", EventCallback.Factory.Create(this, AddTabPane));
            //        b.CloseComponent();
            //    };
            //}

            //_barClassMapper.Clear()
            //    .Add($"{PrefixCls}-bar")
            //    .Add($"{PrefixCls}-{TabPosition}-bar")
            //    .Add($"{PrefixCls}-{Type}-bar")
            //    .If($"{PrefixCls}-{TabType.Card}-bar", () => Type == TabType.EditableCard)
            //    .If($"{PrefixCls}-large-bar", () => Size == TabSize.Large)
            //    .If($"{PrefixCls}-small-bar", () => Size == TabSize.Small);

            //_prevClassMapper.Clear()
            //    .Add($"{PrefixCls}-tab-prev")
            //    .If($"{PrefixCls}-tab-btn-disabled", () => !_prevIconEnabled.HasValue || !_prevIconEnabled.Value)
            //    .If($"{PrefixCls}-tab-arrow-show", () => _prevIconEnabled.HasValue);

            //_nextClassMapper.Clear()
            //    .Add($"{PrefixCls}-tab-next")
            //    .If($"{PrefixCls}-tab-btn-disabled", () => !_nextIconEnabled.HasValue || !_nextIconEnabled.Value)
            //    .If($"{PrefixCls}-tab-arrow-show", () => _nextIconEnabled.HasValue);

            //_navClassMapper.Clear()
            //    .Add($"{PrefixCls}-nav-container")
            //    .If($"{PrefixCls}-nav-container-scrolling", () => _prevIconEnabled.HasValue || _nextIconEnabled.HasValue);

            //_navStyle = "transform: translate3d(0px, 0px, 0px);";
            _inkStyle = "left: 0px; width: 0px;";
            //_contentStyle = "margin-" + (IsHorizontal ? "left" : "top") + ": 0;";
        }

        /// <summary>
        /// Add <see cref="TabPane"/> to <see cref="Tabs"/>
        /// </summary>
        /// <param name="tabPane">The AntTabPane to be added</param>
        /// <exception cref="ArgumentNullException">Key is null</exception>
        /// <exception cref="ArgumentException">An AntTabPane with the same key already exists</exception>
        internal void AddTabPane(TabPane tabPane)
        {
            if (string.IsNullOrEmpty(tabPane.Key))
            {
                throw new ArgumentNullException(nameof(tabPane), "Key is null");
            }

            if (_panes.Select(p => p.Key).Contains(tabPane.Key))
            {
                throw new ArgumentException("An AntTabPane with the same key already exists");
            }

            _panes.Add(tabPane);
        }

        internal void Complete(TabPane content)
        {
            var pane = _panes.FirstOrDefault(x => x.Key == content.Key);
            if (pane != null && pane.IsComplete())
            {
                if (_panes.Any(x => !x.IsComplete()))
                {
                    return;
                }

                if (!string.IsNullOrWhiteSpace(ActiveKey))
                {
                    var activedPane = _panes.Find(x => x.Key == ActiveKey);
                    if (activedPane?.IsActive == false)
                    {
                        ActivatePane(activedPane);
                    }
                }
                else if (!string.IsNullOrWhiteSpace(DefaultActiveKey))
                {
                    var defaultPane = _panes.FirstOrDefault(x => x.Key == DefaultActiveKey);
                    if (defaultPane != null)
                    {
                        ActivatePane(defaultPane);
                    }
                }

                if (_activePane == null || _panes.All(x => !x.IsActive))
                {
                    ActivatePane(_panes.FirstOrDefault());
                }

                if (AfterTabCreated.HasDelegate)
                {
                    AfterTabCreated.InvokeAsync(pane.Key);
                }
            }
        }

        public async Task RemoveTabPane(TabPane pane)
        {
            if (await OnEdit.Invoke(pane.Key, "remove"))
            {
                var index = _panes.IndexOf(pane);
                _panes.Remove(pane);
                if (pane != null && pane.IsActive && _panes.Count > 0)
                {
                    ActivatePane(index > 1 ? _panes[index - 1] : _panes[0]);
                }

                _needRefresh = true;
                StateHasChanged();
            }
        }

        internal void HandleTabClick(TabPane tabPane)
        {
            if (tabPane.IsActive)
                return;

            if (OnTabClick.HasDelegate)
            {
                OnTabClick.InvokeAsync(tabPane.Key);
            }

            ActivatePane(tabPane);
        }

        private void ActivatePane(TabPane tabPane)
        {
            if (!tabPane.Disabled && _panes.Contains(tabPane))
            {
                if (_activePane != null)
                {
                    _activePane.IsActive = false;
                }
                tabPane.IsActive = true;
                _activePane = tabPane;
                if (_activeKey != _activePane.Key)
                {
                    if (!string.IsNullOrEmpty(_activeKey))
                    {
                        if (ActiveKeyChanged.HasDelegate)
                        {
                            ActiveKeyChanged.InvokeAsync(_activePane.Key);
                        }

                        if (OnChange.HasDelegate)
                        {
                            OnChange.InvokeAsync(_activePane.Key);
                        }
                    }

                    _activeKey = _activePane.Key;
                }

                _needRefresh = true;
                _activePaneChanged = true;

                Card?.SetBody(_activePane.ChildContent);

                StateHasChanged();
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);
            if (firstRender)
            {
                _afterFirstRender = true;
            }

            if (_afterFirstRender && _activePane != null)
            {
                await TryRenderInk();
                await TryRenderNavOperation();
            }
            _needRefresh = false;
        }

        private async Task TryRenderNavOperation()
        {
            int navWidth = (await JsInvokeAsync<Element>(JSInteropConstants.GetDomInfo, _tabBars)).clientWidth;
            int navTotalWidth = (await JsInvokeAsync<Element>(JSInteropConstants.GetDomInfo, _scrollTabBar)).clientWidth;
            if (navTotalWidth < navWidth)
            {
                _operationClass = "ant-tabs-nav-operations ant-tabs-nav-operations-hidden";
                _operationStyle = "visibility: hidden; order: 1;";
                _tabsNavWarpPingClass = string.Empty;
            }
            else
            {
                _operationClass = "ant-tabs-nav-operations";
                _tabsNavWarpPingClass = "ant-tabs-nav-wrap-ping-right";
                _operationStyle = string.Empty;
            }

            StateHasChanged();
        }

        private async Task TryRenderInk()
        {
            if (_renderedActivePane == _activePane)
            {
                return;
            }

            await Task.Delay(100);

            // TODO: slide to activated tab
            // animate Active Ink
            // ink bar
            var element = await JsInvokeAsync<Element>(JSInteropConstants.GetDomInfo, _activePane.TabBar);
            var navSection = await JsInvokeAsync<Element>(JSInteropConstants.GetDomInfo, _tabBars);

            if (IsHorizontal)
            {
                //_inkStyle = "left: 0px; width: 0px;";
                _inkStyle = $"left: {element.offsetLeft}px; width: {element.clientWidth}px";
                if (element.offsetLeft > _scrollOffset + navSection.clientWidth
                    || element.offsetLeft < _scrollOffset)
                {
                    // need to scroll tab bars
                    _scrollOffset = element.offsetLeft;
                    _navStyle = $"transform: translate(-{_scrollOffset}px, 0px);";
                }
            }
            else
            {
                _inkStyle = $"top: {element.offsetTop}px; height: {element.clientHeight}px;";
                if (element.offsetTop > _scrollOffset + navSection.clientHeight
                    || element.offsetTop < _scrollOffset)
                {
                    // need to scroll tab bars
                    _scrollOffset = element.offsetTop;
                    _navStyle = $"transform: translate(0px, -{_scrollOffset}px);";
                }
            }
            StateHasChanged();
            _renderedActivePane = _activePane;
        }

        //private async void OnPrevClicked()
        //{
        //    _needRefresh = true;
        //    if (OnPrevClick.HasDelegate)
        //    {
        //        await OnPrevClick.InvokeAsync(null);
        //    }

        //    // get the old offset to the left, and _navIndex != 0 because prev will be disabled
        //    int left = _navIndex * _navSection;
        //    if (IsHorizontal)
        //    {
        //        _navSection = (await JsInvokeAsync<Element>(JSInteropConstants.getDomInfo, _scrollTabBar)).clientWidth;
        //        _navTotal = (await JsInvokeAsync<Element>(JSInteropConstants.getDomInfo, _tabBars)).clientWidth;
        //    }
        //    else
        //    {
        //        _navSection = (await JsInvokeAsync<Element>(JSInteropConstants.getDomInfo, _scrollTabBar)).clientHeight;
        //        _navTotal = (await JsInvokeAsync<Element>(JSInteropConstants.getDomInfo, _tabBars)).clientHeight;
        //    }
        //    // calculate the current _navIndex after users resize the browser, and _navIndex > 0 guaranteed since left > 0
        //    _navIndex = (int)Math.Ceiling(1.0 * left / _navSection);
        //    int offset = --_navIndex * _navSection;
        //    if (IsHorizontal)
        //    {
        //        _navStyle = $"transform: translate3d(-{offset}px, 0px, 0px);";
        //    }
        //    else
        //    {
        //        _navStyle = $"transform: translate3d(0px, -{offset}px, 0px);";
        //    }
        //    RefreshNavIcon();
        //    _needRefresh = false;
        //}

        //private async void OnNextClicked()
        //{
        //    // BUG: when vertical
        //    _needRefresh = true;
        //    if (OnNextClick.HasDelegate)
        //    {
        //        await OnNextClick.InvokeAsync(null);
        //    }

        //    // get the old offset to the left
        //    int left = _navIndex * _navSection;
        //    if (IsHorizontal)
        //    {
        //        _navSection = (await JsInvokeAsync<Element>(JSInteropConstants.getDomInfo, _scrollTabBar)).clientWidth;
        //        _navTotal = (await JsInvokeAsync<Element>(JSInteropConstants.getDomInfo, _tabBars)).clientWidth;
        //    }
        //    else
        //    {
        //        _navSection = (await JsInvokeAsync<Element>(JSInteropConstants.getDomInfo, _scrollTabBar)).clientHeight;
        //        _navTotal = (await JsInvokeAsync<Element>(JSInteropConstants.getDomInfo, _tabBars)).clientHeight;
        //    }
        //    // calculate the current _navIndex after users resize the browser
        //    _navIndex = left / _navSection;
        //    int offset = Math.Min(++_navIndex * _navSection, _navTotal / _navSection * _navSection);
        //    if (IsHorizontal)
        //    {
        //        _navStyle = $"transform: translate3d(-{offset}px, 0px, 0px);";
        //    }
        //    else
        //    {
        //        _navStyle = $"transform: translate3d(0px, -{offset}px, 0px);";
        //    }
        //    RefreshNavIcon();
        //    _needRefresh = false;
        //}

        //private void RefreshNavIcon()
        //{
        //    if (_navTotal > _navSection)
        //    {
        //        if (_navIndex == 0)
        //        {
        //            // reach the first section
        //            _prevIconEnabled = false;
        //        }
        //        else
        //        {
        //            _prevIconEnabled = true;
        //        }

        //        if ((_navIndex + 1) * _navSection > _navTotal)
        //        {
        //            // reach the last section
        //            _nextIconEnabled = false;
        //        }
        //        else
        //        {
        //            _nextIconEnabled = true;
        //        }
        //    }
        //    else
        //    {
        //        // hide icon
        //        _prevIconEnabled = null;
        //        _nextIconEnabled = null;
        //    }

        //    StateHasChanged();
        //}

        protected override bool ShouldRender()
        {
            return _needRefresh || _renderedActivePane != _activePane;
        }

        #region DRAG & DROP

        private TabPane _draggingPane;

        internal void HandleDragStart(DragEventArgs args, TabPane pane)
        {
            if (Draggable)
            {
                args.DataTransfer.DropEffect = "move";
                args.DataTransfer.EffectAllowed = "move";
                _draggingPane = pane;
            }
        }

        internal void HandleDrop(TabPane pane)
        {
            if (Draggable && _draggingPane != null)
            {
                int newIndex = _panes.IndexOf(pane);
                _panes.Remove(_draggingPane);
                _panes.Insert(newIndex, _draggingPane);
                _draggingPane = null;
                _needRefresh = true;
                _renderedActivePane = null;
                StateHasChanged();
            }
        }

        #endregion DRAG & DROP
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;

[RequireComponent(typeof(UIDocument))]
public class RPG_TalentTreeGenerator : MonoBehaviour
{
    [Header("Tree Data")]
    public RPG_TalentTreeData treeData;
    public TalentManager talentManager;
    
    [Header("UI Assets")]
    public VisualTreeAsset treeUXML;
    public VisualTreeAsset nodeUXML;
    public StyleSheet treeUSS;
    
    [Header("Styling")]
    public Color lockedColor = Color.gray;
    public Color availableColor = Color.white;
    public Color allocatedColor = Color.green;
    public Color keystoneColor = Color.gold;
    
    // UI Elements
    VisualElement root;
    VisualElement nodesContainer;
    VisualElement connectionsContainer;
    VisualElement infoPanel;
    Label nodeInfoTitle;
    Label nodeInfoDescription;
    Label nodeInfoStats;
    Label nodeInfoRequirements;
    Button allocateButton;
    Button deallocateButton;
    
    // Runtime data
    Dictionary<RPG_TalentNodeData, VisualElement> nodeElements = new();
    RPG_TalentNodeData selectedNode;
    
    // Navigation
    bool isPanning;
    Vector2 lastMouse;
    float zoom = 1f;
    Vector2 pan;

    void Awake()
    {
        var doc = GetComponent<UIDocument>();
        root = doc.rootVisualElement;
        SetupUI();
        
        // Находим TalentManager если не назначен
        if (talentManager == null)
            talentManager = FindObjectOfType<TalentManager>();
    }
    
    void Start()
    {
        GenerateTree();
        SetupEventListeners();
        UpdateAllNodeStyles();
        
        // Показываем статистику дерева
        if (treeData != null)
        {
            var stats = treeData.GetTreeStats();
            Debug.Log($"Generated talent tree '{treeData.treeName}': {stats}");
        }
    }
    
    void SetupUI()
    {
        root.Clear();
        if (treeUSS != null) root.styleSheets.Add(treeUSS);
        
        if (treeUXML != null)
        {
            var tree = treeUXML.CloneTree();
            root.Add(tree);
            
            nodesContainer = tree.Q<VisualElement>("NodesContainer");
            connectionsContainer = tree.Q<VisualElement>("ConnectionsContainer");
            infoPanel = tree.Q<VisualElement>("InfoPanel");
        }
        else
        {
            // Создаем базовый UI если нет UXML
            var mainContainer = new VisualElement();
            mainContainer.style.flexDirection = FlexDirection.Row;
            mainContainer.style.width = Length.Percent(100);
            mainContainer.style.height = Length.Percent(100);
            
            // Контейнер дерева
            var treeContainer = new VisualElement();
            treeContainer.style.width = Length.Percent(75);
            treeContainer.style.height = Length.Percent(100);
            treeContainer.style.position = Position.Relative;
            
            connectionsContainer = new VisualElement();
            connectionsContainer.name = "ConnectionsContainer";
            connectionsContainer.pickingMode = PickingMode.Ignore;
            
            nodesContainer = new VisualElement();
            nodesContainer.name = "NodesContainer";
            
            treeContainer.Add(connectionsContainer);
            treeContainer.Add(nodesContainer);
            
            // Панель информации
            infoPanel = new VisualElement();
            infoPanel.style.width = Length.Percent(25);
            infoPanel.style.height = Length.Percent(100);
            infoPanel.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            infoPanel.style.paddingTop = 10;
            infoPanel.style.paddingLeft = 10;
            infoPanel.style.paddingRight = 10;
            infoPanel.style.paddingBottom = 10;
            
            SetupInfoPanel();
            
            mainContainer.Add(treeContainer);
            mainContainer.Add(infoPanel);
            root.Add(mainContainer);
        }
        
        // Настраиваем навигацию
        root.RegisterCallback<WheelEvent>(OnWheel);
        root.RegisterCallback<MouseDownEvent>(OnMouseDown);
        root.RegisterCallback<MouseMoveEvent>(OnMouseMove);
        root.RegisterCallback<MouseUpEvent>(OnMouseUp);
    }
    
    void SetupInfoPanel()
    {
        nodeInfoTitle = new Label("Select a talent node");
        nodeInfoTitle.style.fontSize = 16;
        nodeInfoTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
        nodeInfoTitle.style.marginBottom = 10;
        
        nodeInfoDescription = new Label("");
        nodeInfoDescription.style.whiteSpace = WhiteSpace.Normal;
        nodeInfoDescription.style.marginBottom = 10;
        
        nodeInfoStats = new Label("");
        nodeInfoStats.style.whiteSpace = WhiteSpace.Normal;
        nodeInfoStats.style.marginBottom = 10;
        nodeInfoStats.style.color = Color.green;
        
        nodeInfoRequirements = new Label("");
        nodeInfoRequirements.style.whiteSpace = WhiteSpace.Normal;
        nodeInfoRequirements.style.marginBottom = 10;
        nodeInfoRequirements.style.color = Color.yellow;
        
        allocateButton = new Button { text = "Allocate" };
        allocateButton.style.marginBottom = 5;
        allocateButton.clicked += OnAllocateClicked;
        
        deallocateButton = new Button { text = "Deallocate" };
        deallocateButton.clicked += OnDeallocateClicked;
        
        infoPanel.Add(nodeInfoTitle);
        infoPanel.Add(nodeInfoDescription);
        infoPanel.Add(nodeInfoStats);
        infoPanel.Add(nodeInfoRequirements);
        infoPanel.Add(allocateButton);
        infoPanel.Add(deallocateButton);
        
        UpdateInfoPanel(null);
    }
    
    void SetupEventListeners()
    {
        if (talentManager != null)
        {
            talentManager.OnTalentAllocated += OnTalentChanged;
            talentManager.OnTalentDeallocated += OnTalentChanged;
            talentManager.OnTalentPointsChanged += OnTalentPointsChanged;
        }
    }
    
    void OnTalentChanged(RPG_TalentNodeData node)
    {
        UpdateAllNodeStyles();
        if (selectedNode != null)
            UpdateInfoPanel(selectedNode);
    }
    
    void OnTalentPointsChanged(int points)
    {
        // Обновляем доступность узлов
        UpdateAllNodeStyles();
        if (selectedNode != null)
            UpdateInfoPanel(selectedNode);
    }

    void GenerateTree()
    {
        if (treeData == null || nodeUXML == null) return;
        
        // Очищаем предыдущие элементы
        nodesContainer.Clear();
        connectionsContainer.Clear();
        nodeElements.Clear();
        
        // Создаем узлы
        foreach (var node in treeData.nodes)
        {
            CreateNodeElement(node);
        }
        
        // Создаем соединения
        CreateConnections();
    }
    
    void CreateNodeElement(RPG_TalentNodeData node)
    {
        var nodeElement = nodeUXML.CloneTree().Q<Button>("TalentNode");
        if (nodeElement == null)
        {
            // Создаем простой узел если нет UXML
            nodeElement = new Button();
            nodeElement.name = "TalentNode";
            nodeElement.style.width = 48;
            nodeElement.style.height = 48;
            nodeElement.style.borderTopLeftRadius = 24;
            nodeElement.style.borderTopRightRadius = 24;
            nodeElement.style.borderBottomLeftRadius = 24;
            nodeElement.style.borderBottomRightRadius = 24;
            
            var label = new Label(node.nodeName.Substring(0, 1));
            label.style.alignSelf = Align.Center;
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            nodeElement.Add(label);
        }
        
        nodeElement.userData = node;
        nodeElement.tooltip = GetNodeTooltip(node);
        
        // Устанавливаем иконку и текст
        var icon = nodeElement.Q<VisualElement>("Icon");
        var nameLabel = nodeElement.Q<Label>("NodeName");
        var typeLabel = nodeElement.Q<Label>("NodeType");
        
        if (icon != null && node.icon != null)
            icon.style.backgroundImage = new StyleBackground(node.icon);
            
        if (nameLabel != null)
            nameLabel.text = node.nodeName;
            
        if (typeLabel != null)
            typeLabel.text = node.nodeType.ToString();
        
        // Позиционируем узел
        PositionNode(nodeElement, node);
        
        // Обработчики событий
        nodeElement.RegisterCallback<ClickEvent>(evt => OnNodeClick(node));
        nodeElement.RegisterCallback<MouseEnterEvent>(evt => OnNodeHover(nodeElement, node, true));
        nodeElement.RegisterCallback<MouseLeaveEvent>(evt => OnNodeHover(nodeElement, node, false));
        
        // Добавляем в контейнер
        nodeElements[node] = nodeElement;
        nodesContainer.Add(nodeElement);
    }
    
    void CreateConnections()
    {
        foreach (var kvp in nodeElements)
        {
            var node = kvp.Key;
            var nodeElement = kvp.Value;
            
            foreach (var prereq in node.prerequisites)
            {
                if (nodeElements.ContainsKey(prereq))
                {
                    var line = new ConnectionLine(nodeElements[prereq], nodeElement);
                    connectionsContainer.Add(line);
                    line.SendToBack();
                }
            }
        }
    }

    void PositionNode(VisualElement element, RPG_TalentNodeData node)
    {
        float radius = node.ringIndex * treeData.ringSpacing;
        float radians = node.angle * Mathf.Deg2Rad;
        
        // Центрируем относительно контейнера
        float centerX = treeData.treeCenter.x;
        float centerY = treeData.treeCenter.y;
        
        float x = centerX + radius * Mathf.Cos(radians) - 24; // 24 = половина размера узла
        float y = centerY + radius * Mathf.Sin(radians) - 24;
        
        element.style.position = Position.Absolute;
        element.style.left = x;
        element.style.top = y;
    }

    void OnNodeClick(RPG_TalentNodeData node)
    {
        selectedNode = node;
        UpdateInfoPanel(node);
        UpdateNodeSelectionStyles();
    }
    
    void OnAllocateClicked()
    {
        if (selectedNode != null && talentManager != null)
        {
            talentManager.TryAllocateTalent(selectedNode);
        }
    }
    
    void OnDeallocateClicked()
    {
        if (selectedNode != null && talentManager != null)
        {
            talentManager.TryDeallocateTalent(selectedNode);
        }
    }
    
    void UpdateInfoPanel(RPG_TalentNodeData node)
    {
        if (node == null)
        {
            nodeInfoTitle.text = "Select a talent node";
            nodeInfoDescription.text = "";
            nodeInfoStats.text = "";
            nodeInfoRequirements.text = "";
            allocateButton.SetEnabled(false);
            deallocateButton.SetEnabled(false);
            return;
        }
        
        nodeInfoTitle.text = $"{node.nodeName} ({node.nodeType})";
        nodeInfoDescription.text = node.description;
        nodeInfoStats.text = node.GetStatsPreview();
        
        // Требования
        var requirements = new List<string>();
        if (node.requiredLevel > 1)
            requirements.Add($"Level {node.requiredLevel}");
        if (!node.requiresAllArchetypes)
            requirements.Add($"{node.requiredArchetype} archetype");
        if (node.prerequisites.Count > 0)
            requirements.Add($"Prerequisites: {string.Join(", ", node.prerequisites.Select(p => p.nodeName))}");
        if (node.talentPointCost > 1)
            requirements.Add($"Cost: {node.talentPointCost} points");
            
        nodeInfoRequirements.text = requirements.Count > 0 ? string.Join("\n", requirements) : "No requirements";
        
        // Кнопки
        if (talentManager != null)
        {
            bool isAllocated = talentManager.IsAllocated(node);
            bool canAllocate = talentManager.CanAllocateTalent(node, out _);
            bool canDeallocate = talentManager.CanDeallocateTalent(node, out _);
            
            allocateButton.SetEnabled(!isAllocated && canAllocate);
            deallocateButton.SetEnabled(isAllocated && canDeallocate);
        }
        else
        {
            allocateButton.SetEnabled(false);
            deallocateButton.SetEnabled(false);
        }
    }

    void UpdateAllNodeStyles()
    {
        foreach (var kvp in nodeElements)
        {
            UpdateNodeStyle(kvp.Value, kvp.Key);
        }
    }
    
    void UpdateNodeSelectionStyles()
    {
        foreach (var kvp in nodeElements)
        {
            var element = kvp.Value;
            var node = kvp.Key;
            
            element.RemoveFromClassList("selected");
            if (node == selectedNode)
                element.AddToClassList("selected");
        }
    }

    void UpdateNodeStyle(VisualElement element, RPG_TalentNodeData node)
    {
        // Убираем все стили состояния
        element.RemoveFromClassList("locked");
        element.RemoveFromClassList("available");
        element.RemoveFromClassList("allocated");
        element.RemoveFromClassList("keystone");
        element.RemoveFromClassList("notable");
        element.RemoveFromClassList("minor");
        
        // Добавляем стиль типа узла
        element.AddToClassList(node.nodeType.ToString().ToLower());
        
        if (talentManager == null)
        {
            element.AddToClassList("locked");
            return;
        }
        
        bool isAllocated = talentManager.IsAllocated(node);
        bool canAllocate = talentManager.CanAllocateTalent(node, out _);
        
        if (isAllocated)
        {
            element.AddToClassList("allocated");
            if (node.isKeystone)
                element.AddToClassList("keystone");
        }
        else if (canAllocate)
        {
            element.AddToClassList("available");
        }
        else
        {
            element.AddToClassList("locked");
        }
    }

    void OnNodeHover(VisualElement element, RPG_TalentNodeData node, bool isHovering)
    {
        if (isHovering)
        {
            element.AddToClassList("hovered");
            // Можно показать превью связанных узлов
            HighlightConnectedNodes(node, true);
        }
        else
        {
            element.RemoveFromClassList("hovered");
            HighlightConnectedNodes(node, false);
        }
    }
    
    void HighlightConnectedNodes(RPG_TalentNodeData node, bool highlight)
    {
        // Подсвечиваем предварительные условия
        foreach (var prereq in node.prerequisites)
        {
            if (nodeElements.ContainsKey(prereq))
            {
                var element = nodeElements[prereq];
                if (highlight)
                    element.AddToClassList("prerequisite");
                else
                    element.RemoveFromClassList("prerequisite");
            }
        }
        
        // Подсвечиваем зависимые узлы
        foreach (var kvp in nodeElements)
        {
            if (kvp.Key.prerequisites.Contains(node))
            {
                if (highlight)
                    kvp.Value.AddToClassList("dependent");
                else
                    kvp.Value.RemoveFromClassList("dependent");
            }
        }
    }
    
    string GetNodeTooltip(RPG_TalentNodeData node)
    {
        var tooltip = $"<b>{node.nodeName}</b>\n{node.description}";
        
        var stats = node.GetStatsPreview();
        if (!string.IsNullOrEmpty(stats))
            tooltip += $"\n\n<color=green>{stats}</color>";
            
        if (node.talentPointCost > 1)
            tooltip += $"\n\n<color=yellow>Cost: {node.talentPointCost} points</color>";
            
        return tooltip;
    }

    // Навигация
    void OnWheel(WheelEvent evt)
    {
        zoom = Mathf.Clamp(zoom - evt.delta.y * 0.01f, 0.3f, 3f);
        ApplyTransform();
    }

    void OnMouseDown(MouseDownEvent evt)
    {
        if (evt.button == 0)
        {
            isPanning = true;
            lastMouse = evt.localMousePosition;
            root.CaptureMouse();
        }
    }

    void OnMouseMove(MouseMoveEvent evt)
    {
        if (!isPanning || !root.HasMouseCapture()) return;
        
        Vector2 delta = evt.localMousePosition - lastMouse;
        lastMouse = evt.localMousePosition;
        pan += delta;
        ApplyTransform();
    }

    void OnMouseUp(MouseUpEvent evt)
    {
        if (evt.button == 0 && root.HasMouseCapture())
        {
            isPanning = false;
            root.ReleaseMouse();
        }
    }

    void ApplyTransform()
    {
        var scale = new Vector3(zoom, zoom, 1);
        var position = new Vector3(pan.x, pan.y, 0);
        
        nodesContainer.transform.scale = scale;
        nodesContainer.transform.position = position;
        connectionsContainer.transform.scale = scale;
        connectionsContainer.transform.position = position;
    }
    
    void OnDestroy()
    {
        if (talentManager != null)
        {
            talentManager.OnTalentAllocated -= OnTalentChanged;
            talentManager.OnTalentDeallocated -= OnTalentChanged;
            talentManager.OnTalentPointsChanged -= OnTalentPointsChanged;
        }
    }

    // Вложенный класс для соединительных линий
    class ConnectionLine : VisualElement
    {
        VisualElement start;
        VisualElement end;
        
        public ConnectionLine(VisualElement startNode, VisualElement endNode)
        {
            start = startNode;
            end = endNode;
            pickingMode = PickingMode.Ignore;
            generateVisualContent += OnGenerate;
            
            style.position = Position.Absolute;
            style.left = 0;
            style.top = 0;
            style.right = 0;
            style.bottom = 0;
        }

        void OnGenerate(MeshGenerationContext ctx)
        {
            if (start == null || end == null) return;
            
            var startCenter = start.worldBound.center;
            var endCenter = end.worldBound.center;
            
            // Преобразуем в локальные координаты
            var localStart = this.WorldToLocal(startCenter);
            var localEnd = this.WorldToLocal(endCenter);
            
            var painter = ctx.painter2D;
            painter.lineWidth = 2;
            painter.strokeColor = Color.white;
            painter.BeginPath();
            painter.MoveTo(localStart);
            painter.LineTo(localEnd);
            painter.Stroke();
        }
    }
}

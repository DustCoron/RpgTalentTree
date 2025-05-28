using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class RPG_TalentTreeGenerator : MonoBehaviour
{
    public RPG_TalentTreeData treeData;
    public VisualTreeAsset treeUXML;
    public VisualTreeAsset nodeUXML;
    public StyleSheet treeUSS;

    VisualElement root;
    VisualElement nodesContainer;
    VisualElement connectionsContainer;
    Dictionary<RPG_TalentNodeData, VisualElement> nodeElements = new();
    HashSet<RPG_TalentNodeData> unlocked = new();

    public RPG_Basic_Stats CalculateTotalStats()
    {
        var total = new RPG_Basic_Stats();
        foreach (var node in unlocked)
        {
            if (node.stats != null)
                total += node.stats;
        }
        return total;
    }

    bool isPanning;
    Vector2 lastMouse;
    float zoom = 1f;
    Vector2 pan;

    void Awake()
    {
        var doc = GetComponent<UIDocument>();
        root = doc.rootVisualElement;
        root.Clear();
        if (treeUSS != null) root.styleSheets.Add(treeUSS);
        if (treeUXML != null)
        {
            var tree = treeUXML.CloneTree();
            root.Add(tree);
            nodesContainer = tree.Q<VisualElement>("NodesContainer");
            connectionsContainer = tree.Q<VisualElement>("ConnectionsContainer");
        }
        else
        {
            nodesContainer = new VisualElement();
            connectionsContainer = new VisualElement();
            root.Add(connectionsContainer);
            root.Add(nodesContainer);
        }
        root.RegisterCallback<WheelEvent>(OnWheel);
        root.RegisterCallback<MouseDownEvent>(OnMouseDown);
        root.RegisterCallback<MouseMoveEvent>(OnMouseMove);
        root.RegisterCallback<MouseUpEvent>(OnMouseUp);
        GenerateTree();
    }

    void GenerateTree()
    {
        if (treeData == null || nodeUXML == null) return;
        foreach (var node in treeData.nodes)
        {
            var el = nodeUXML.CloneTree().Q<Button>("TalentNode");
            el.userData = node;
            el.tooltip = $"{node.nodeName}\n{node.description}";
            el.AddToClassList("talent-node");
            el.Q<Label>("NodeName").text = node.nodeName;
            // Optionally set icon here: el.Q<Image>("Icon").image = ...
            UpdateNodeStyle(el);
            PositionNode(el, node);
            el.RegisterCallback<ClickEvent>(evt => OnNodeClick(node));
            el.RegisterCallback<MouseEnterEvent>(evt => OnNodeHover(el, node, true));
            el.RegisterCallback<MouseLeaveEvent>(evt => OnNodeHover(el, node, false));
            nodeElements[node] = el;
            nodesContainer.Add(el);
        }
        // connections
        foreach (var kv in nodeElements)
        {
            var node = kv.Key;
            foreach (var prereq in node.prerequisites)
            {
                if (nodeElements.ContainsKey(prereq))
                {
                    var line = new ConnectionLine(nodeElements[prereq], kv.Value);
                    connectionsContainer.Add(line);
                    line.SendToBack();
                }
            }
        }
    }

    void PositionNode(VisualElement el, RPG_TalentNodeData data)
    {
        float radius = data.ringIndex * treeData.ringSpacing;
        float rad = data.angle * Mathf.Deg2Rad;
        // Calculate center based on container size
        float centerX = 0.5f * nodesContainer.resolvedStyle.width;
        float centerY = 0.5f * nodesContainer.resolvedStyle.height;
        float x = centerX + radius * Mathf.Cos(rad) - 24; // 24 = half node size
        float y = centerY + radius * Mathf.Sin(rad) - 24;
        el.style.position = Position.Absolute;
        el.style.left = x;
        el.style.top = y;
    }

    void OnNodeClick(RPG_TalentNodeData node)
    {
        if (unlocked.Contains(node)) return;
        foreach (var pre in node.prerequisites)
            if (!unlocked.Contains(pre)) return;
        unlocked.Add(node);
        UpdateNodeStyle(nodeElements[node]);
        Debug.Log("Total Stats: " + CalculateTotalStats());
    }

    void UpdateNodeStyle(VisualElement el)
    {
        var data = (RPG_TalentNodeData)el.userData;
        el.RemoveFromClassList("locked");
        el.RemoveFromClassList("unlocked");
        bool isUnlocked = unlocked.Contains(data);
        el.AddToClassList(isUnlocked ? "unlocked" : "locked");
    }

    void OnNodeHover(VisualElement el, RPG_TalentNodeData node, bool hover)
    {
        if (hover) el.AddToClassList("selected");
        else el.RemoveFromClassList("selected");
        // Optionally show stat preview, etc.
    }

    void OnWheel(WheelEvent evt)
    {
        zoom = Mathf.Clamp(zoom - evt.delta.y * 0.01f, 0.5f, 2f);
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
        nodesContainer.transform.scale = new Vector3(zoom, zoom, 1);
        nodesContainer.transform.position = new Vector3(pan.x, pan.y, 0);
        connectionsContainer.transform.scale = new Vector3(zoom, zoom, 1);
        connectionsContainer.transform.position = new Vector3(pan.x, pan.y, 0);
    }

    class ConnectionLine : VisualElement
    {
        VisualElement start;
        VisualElement end;
        public ConnectionLine(VisualElement a, VisualElement b)
        {
            start = a;
            end = b;
            pickingMode = PickingMode.Ignore;
            generateVisualContent += OnGenerate;
        }

        void OnGenerate(MeshGenerationContext ctx)
        {
            if (start == null || end == null) return;
            Vector2 s = start.worldBound.center;
            Vector2 e = end.worldBound.center;
            var painter = ctx.painter2D;
            painter.lineWidth = 2;
            painter.strokeColor = Color.white;
            painter.BeginPath();
            painter.MoveTo(s);
            painter.LineTo(e);
            painter.Stroke();
        }
    }
}

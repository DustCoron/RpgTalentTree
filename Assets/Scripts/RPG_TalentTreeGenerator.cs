using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class RPG_TalentTreeGenerator : MonoBehaviour
{
    public RPG_TalentTreeData treeData;
    public Color lockedColor = Color.gray;
    public Color unlockedColor = Color.green;
    public int availablePoints = 10;

    VisualElement root;
    VisualElement container;
    class NodeInstance
    {
        public RPG_TalentNodeData data;
        public int index;
    }

    List<NodeInstance> instances = new List<NodeInstance>();
    Dictionary<NodeInstance, VisualElement> nodeElements = new Dictionary<NodeInstance, VisualElement>();
    HashSet<NodeInstance> unlocked = new HashSet<NodeInstance>();

    public RPG_Basic_Stats CalculateTotalStats()
    {
        var total = new RPG_Basic_Stats();
        foreach (var inst in unlocked)
        {
            if (inst.data.stats != null)
            {
                total += inst.data.stats;
            }
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
        container = new VisualElement();
        container.style.position = Position.Absolute;
        container.style.left = 0;
        container.style.top = 0;
        container.style.width = Length.Percent(100);
        container.style.height = Length.Percent(100);
        root.Add(container);

        root.RegisterCallback<WheelEvent>(OnWheel);
        root.RegisterCallback<MouseDownEvent>(OnMouseDown);
        root.RegisterCallback<MouseMoveEvent>(OnMouseMove);
        root.RegisterCallback<MouseUpEvent>(OnMouseUp);

        GenerateTree();
    }

    void GenerateTree()
    {
        if (treeData == null) return;
        int id = 0;
        foreach (var data in treeData.nodes)
        {
            int count = Mathf.Max(1, data.repeatCount);
            for (int i = 0; i < count; i++)
            {
                var inst = new NodeInstance { data = data, index = id++ };
                instances.Add(inst);
                var el = new Button();
                el.text = string.Empty;
                el.userData = inst;
                el.style.position = Position.Absolute;
                el.style.width = 32;
                el.style.height = 32;
                el.tooltip = $"{data.nodeName}\n{data.description}";
                UpdateNodeStyle(el);
                float angle = data.angle + i * data.repeatSpacing;
                PositionNode(el, data.ringIndex, angle);
                el.RegisterCallback<ClickEvent>(evt => OnNodeClick(inst));
                nodeElements[inst] = el;
                container.Add(el);
            }
        }
        // connections
        foreach (var kv in nodeElements)
        {
            var inst = kv.Key;
            foreach (var prereq in inst.data.prerequisites)
            {
                // connect to first instance of prerequisite
                var target = instances.Find(n => n.data == prereq);
                if (target != null && nodeElements.ContainsKey(target))
                {
                    var line = new ConnectionLine(nodeElements[target], kv.Value);
                    container.Add(line);
                    line.SendToBack();
                }
            }
        }
    }

    void PositionNode(VisualElement el, int ringIndex, float angle)
    {
        float radius = ringIndex * treeData.ringSpacing;
        float rad = angle * Mathf.Deg2Rad;
        el.style.left = container.layout.width / 2 + radius * Mathf.Cos(rad) - el.layout.width / 2;
        el.style.top = container.layout.height / 2 + radius * Mathf.Sin(rad) - el.layout.height / 2;
    }

    void OnNodeClick(NodeInstance inst)
    {
        if (unlocked.Contains(inst)) return;
        foreach (var pre in inst.data.prerequisites)
        {
            var req = instances.Find(n => n.data == pre);
            if (req != null && !unlocked.Contains(req)) return;
        }
        if (inst.data.cost > availablePoints) return;
        availablePoints -= inst.data.cost;
        unlocked.Add(inst);
        UpdateNodeStyle(nodeElements[inst]);
        Debug.Log($"Points left: {availablePoints} Total Stats: {CalculateTotalStats()}");
    }

    void UpdateNodeStyle(VisualElement el)
    {
        var inst = (NodeInstance)el.userData;
        bool isUnlocked = unlocked.Contains(inst);
        el.style.backgroundColor = isUnlocked ? new StyleColor(unlockedColor) : new StyleColor(lockedColor);
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
        container.transform.scale = new Vector3(zoom, zoom, 1);
        container.transform.position = new Vector3(pan.x, pan.y, 0);
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

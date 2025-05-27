using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class TalentTreeGenerator : MonoBehaviour
{
    public TalentTreeData treeData;
    public Color lockedColor = Color.gray;
    public Color unlockedColor = Color.green;

    VisualElement root;
    VisualElement container;
    Dictionary<TalentNodeData, VisualElement> nodeElements = new Dictionary<TalentNodeData, VisualElement>();
    HashSet<TalentNodeData> unlocked = new HashSet<TalentNodeData>();

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
        foreach (var node in treeData.nodes)
        {
            var el = new Button();
            el.text = ""; // Could show icon/text
            el.userData = node;
            el.style.position = Position.Absolute;
            el.style.width = 32;
            el.style.height = 32;
            el.tooltip = $"{node.nodeName}\n{node.description}";
            UpdateNodeStyle(el);
            PositionNode(el, node);
            el.RegisterCallback<ClickEvent>(evt => OnNodeClick(node));
            nodeElements[node] = el;
            container.Add(el);
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
                    container.Add(line);
                    line.SendToBack();
                }
            }
        }
    }

    void PositionNode(VisualElement el, TalentNodeData data)
    {
        float radius = data.ringIndex * treeData.ringSpacing;
        float rad = data.angle * Mathf.Deg2Rad;
        el.style.left = container.layout.width / 2 + radius * Mathf.Cos(rad) - el.layout.width / 2;
        el.style.top = container.layout.height / 2 + radius * Mathf.Sin(rad) - el.layout.height / 2;
    }

    void OnNodeClick(TalentNodeData node)
    {
        if (unlocked.Contains(node)) return;
        foreach (var pre in node.prerequisites)
        {
            if (!unlocked.Contains(pre)) return;
        }
        unlocked.Add(node);
        UpdateNodeStyle(nodeElements[node]);
    }

    void UpdateNodeStyle(VisualElement el)
    {
        var data = (TalentNodeData)el.userData;
        bool isUnlocked = unlocked.Contains(data);
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

using UnityEngine;
using UnityEngine.UIElements;

public class UIToolkitCreateUI : MonoBehaviour
{
    public UIDocument uiDocument;

    void Start()
    {
        var root = uiDocument.rootVisualElement;

        // Кнопка
        var button = new Button(() => Debug.Log("Button clicked!"))
        {
            text = "Нажми меня!"
        };
        root.Add(button);

        // Лейбл
        var label = new Label("Это текстовый лейбл");
        root.Add(label);

        // Стилизация через C# (inline)
        button.style.width = 200;
        button.style.height = 40;
        button.style.marginTop = 20;
        label.style.color = Color.magenta;
    }
}


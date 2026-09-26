using UnityEngine;
using SpatialAwareness.Navigation;

public class NavigationTest : MonoBehaviour
{
    private NavigationGuide guide;
    private ObjectHighlighter highlighter;

    void Start()
    {
        guide = GetComponent<NavigationGuide>();
        highlighter = ObjectHighlighter.Instance;
    }

    void Update()
    {

    }
}

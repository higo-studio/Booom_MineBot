using UnityEngine;

namespace Minebot.Bootstrap
{
    [DefaultExecutionOrder(-200)]
    public sealed class SceneRenderBootstrap : MonoBehaviour
    {
        [SerializeField]
        private Color cameraBackground = new Color(0.08f, 0.1f, 0.12f, 1f);

        [SerializeField]
        private Color placeholderColor = new Color(0.9f, 0.62f, 0.18f, 1f);

        [SerializeField]
        private bool createPlaceholderWhenEmpty = true;

        private void Awake()
        {
            EnsureCamera();
            if (createPlaceholderWhenEmpty)
            {
                EnsureVisiblePlaceholder();
            }
        }

        private void EnsureCamera()
        {
            if (Camera.main != null)
            {
                return;
            }

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = cameraBackground;

            cameraObject.AddComponent<AudioListener>();
        }

        private void EnsureVisiblePlaceholder()
        {
            if (FindAnyObjectByType<Renderer>() != null)
            {
                return;
            }

            var marker = GameObject.CreatePrimitive(PrimitiveType.Quad);
            marker.name = $"{gameObject.scene.name} Render Marker";
            marker.transform.position = Vector3.zero;
            marker.transform.localScale = new Vector3(2f, 2f, 1f);

            Renderer renderer = marker.GetComponent<Renderer>();
            renderer.sharedMaterial = CreatePlaceholderMaterial();
        }

        private Material CreatePlaceholderMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }

            var material = new Material(shader)
            {
                color = placeholderColor
            };
            return material;
        }
    }
}

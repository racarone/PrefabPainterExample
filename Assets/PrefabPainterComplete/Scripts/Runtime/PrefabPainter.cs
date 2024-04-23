using UnityEngine;

namespace Example
{
    /// <summary>
    /// Controls how the way prefab should be placed.
    /// </summary>
    public enum PrefabPlacementMode
    {
        Single,
        Circle,
    }
    
    /// <summary>
    /// Our prefab painter component.
    /// </summary>
    public class PrefabPainter : MonoBehaviour
    {
        // Header attribute groups fields together in the inspector automatically.
        // Tooltip attribute displays a tooltip in the inspector.
        
        [Header("General")]
        
        [Tooltip("The prefab to paint.")]
        public GameObject Prefab;
        
        [Tooltip("The parent object to place painted prefabs under.")]
        public GameObject Parent;
        
        
        [Header("Placement Type")]
        
        [Tooltip("How the painted prefabs should be placed.")]
        public PrefabPlacementMode PlacementMode = PrefabPlacementMode.Single;
        
        [Tooltip("The radius of the circle to place painted prefabs in, if PlaceMode is set to Circle."), Range(0f, 50f)]
        public float PlacementCircleRadius = 5f;
        
        [Tooltip("How many prefabs will be painted within the circle."), Range(0, 100)]
        public int PlacementCircleAmount = 10;
        
        
        [Header("Placement Rotation")]
        
        [Tooltip("If enabled, the painted prefabs will be randomly rotated around the Y-axis.")]
        public bool EnableRandomRotation = true;
        
        
        [Header("Placement Scale")]
        
        [Tooltip("If enabled, the painted prefabs will be randomly scaled.")]
        public bool EnableRandomScaling = true;
        
        [Tooltip("The minimum scale to apply to painted prefabs."), Min(0f)]
        public float MinScale = 0.5f;
        
        [Tooltip("The maximum scale to apply to painted prefabs."), Min(0f)]
        public float MaxScale = 1.5f;
    }
}

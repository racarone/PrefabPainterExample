using UnityEditor;
using UnityEngine;

namespace Example
{
    // Custom editor for the PrefabPainter component
    [CustomEditor(typeof(PrefabPainter))]
    public class PrefabPainterEditor : Editor
    {
        // Serialized properties
        SerializedProperty m_Prefab;
        SerializedProperty m_Parent;
        SerializedProperty m_PlacementMode;
        SerializedProperty m_PlacementCircleRadius;
        SerializedProperty m_PlacementCircleAmount;
        SerializedProperty m_EnableRandomScaling;
        SerializedProperty m_MinScale;
        SerializedProperty m_MaxScale;
        SerializedProperty m_EnableRandomRotation;

        // PrefabPainter reference
        PrefabPainter m_Painter;
        bool m_IsPainting;
        
        void OnEnable()
        {
            // Get the target from the editor, it will always be a PrefabPainter
            m_Painter = (PrefabPainter)target;
            
            // Find the serialized properties from the PrefabPainter component, by name
            m_Prefab = serializedObject.FindProperty("Prefab");
            m_Parent = serializedObject.FindProperty("Parent");
            m_PlacementMode = serializedObject.FindProperty("PlacementMode");
            m_PlacementCircleRadius = serializedObject.FindProperty("PlacementCircleRadius");
            m_PlacementCircleAmount = serializedObject.FindProperty("PlacementCircleAmount");
            m_EnableRandomScaling = serializedObject.FindProperty("EnableRandomScaling");
            m_MinScale = serializedObject.FindProperty("MinScale");
            m_MaxScale = serializedObject.FindProperty("MaxScale");
            m_EnableRandomRotation = serializedObject.FindProperty("EnableRandomRotation");
        }
        
        // `OnInspectorGUI` is called whenever the inspector's properties need to be drawn
        public override void OnInspectorGUI()
        {
            // Updates the 'SerializedObject' with the latest values from the target
            serializedObject.Update();
            
            // Draws a default property field for the 'Prefab' property
            EditorGUILayout.PropertyField(m_Prefab);
            
            // Draws a label with a bold font style, good for section headers
            EditorGUILayout.LabelField("General", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_Parent);
            EditorGUILayout.PropertyField(m_PlacementMode);
            
            // Easy way to indent the following properties
            using (new EditorGUI.IndentLevelScope())
            {
                // Only show the circle placement properties if the PlacementMode is set to Circle
                if (m_PlacementMode.enumValueIndex == (int)PrefabPlacementMode.Circle)
                {
                    EditorGUILayout.PropertyField(m_PlacementCircleRadius);
                    EditorGUILayout.PropertyField(m_PlacementCircleAmount);
                }
            }
            
            // Draw the rest of the properties
            EditorGUILayout.LabelField("Rotation", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_EnableRandomRotation);
            
            EditorGUILayout.LabelField("Scale", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_EnableRandomScaling);
            
            // Similar to indent scope, but disables the GUI elements within the scope if the condition is true.
            using (new EditorGUI.DisabledGroupScope(!m_EnableRandomScaling.boolValue))
            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.PropertyField(m_MinScale);
                EditorGUILayout.PropertyField(m_MaxScale);
            }
            
            serializedObject.ApplyModifiedProperties();
        }

        // `OnSceneGUI` is called during the scene view rendering and is used to draw custom scene handles
        void OnSceneGUI()
        {
            if (m_Painter && m_Painter.Prefab)
            {
                // The current event being processed by the editor
                Event currentEvent = Event.current;
                
                // Can be used to get the mouse position in the scene view
                Vector2 mousePosition = currentEvent.mousePosition;
                
                // Handy utility to convert the mouse position to a world ray that can be used for raycasting
                Ray mouseRay = HandleUtility.GUIPointToWorldRay(mousePosition);
                
                // Raycast to see if the mouse ray hits any colliders in the scene
                bool hitSurface = Physics.Raycast(mouseRay, out RaycastHit surfaceHit);
                
                // Handle the different event types
                switch (currentEvent.type)
                {
                    // MouseDown is called when the user presses a mouse button
                    case EventType.MouseDown:
                        if (hitSurface) // Make sure we're hovering over a surface
                        {
                            if (currentEvent.button == 0) // Left mouse button
                            {
                                // Now we're painting
                                m_IsPainting = true; 
                                
                                // Do the first placement
                                Place(surfaceHit);
                                
                                // Mark the event as used, so it doesn't get handled by other systems (e.g. Unity's default scene view controls)
                                currentEvent.Use();
                            }
                        }
                        break;
                    
                    case EventType.MouseDrag:
                        if (m_IsPainting)
                        {
                            // Dragging the mouse, so keep painting
                            Place(surfaceHit);
                            currentEvent.Use();
                        }
                        break;
                    
                    case EventType.MouseUp:
                        if (m_IsPainting && currentEvent.button == 0)
                        {
                            // Stop painting when the mouse button is released
                            m_IsPainting = false;
                            currentEvent.Use();
                        }
                        break;
                    
                    case EventType.Repaint:
                        // If we're hovering over a surface, draw a visual indicator
                        if (hitSurface)
                        {
                            // Set the color for the handles, green if painting, white otherwise
                            Handles.color = m_IsPainting ? Color.green : Color.white;
                            
                            // Draw a circle or a small point based on the placement mode
                            if (m_Painter.PlacementMode == PrefabPlacementMode.Circle)
                            {
                                Handles.DrawWireDisc(surfaceHit.point, surfaceHit.normal, m_Painter.PlacementCircleRadius);
                            }
                            else
                            {
                                // HandleUtility.GetHandleSize is used to get a size that stays constant in screen space
                                float size = HandleUtility.GetHandleSize(surfaceHit.point) * 0.1f;
                                Handles.DrawSolidDisc(surfaceHit.point, surfaceHit.normal, size);
                            }
                        }
                        break;
                }
            }
        }
        
        // Try to place a prefab at the given hit point, based on the current placement mode
        void Place(RaycastHit hit)
        {
            if (m_Painter.PlacementMode == PrefabPlacementMode.Circle)
            {
                PlacePrefabsInsideCircle(hit);
            }
            else
            {
                CreatePaintedPrefabAt(hit);
            }
        }
        
        // Place multiple prefabs in a circle around the hit point
        void PlacePrefabsInsideCircle(RaycastHit hit)
        {
            float placementRadius = m_Painter.PlacementCircleRadius;
            float placementAmount = m_Painter.PlacementCircleAmount;
            Vector3 circleCenter = hit.point;
            
            for (int i = 0; i < placementAmount; i++)
            {
                // Get a random point inside a unit circle, then scale it by the placement radius
                Vector2 randomPoint = Random.insideUnitCircle * placementRadius;
                
                // Convert the 2D random point to a 3D position by adding it to the circle center
                Vector3 randomPosition = circleCenter + new Vector3(randomPoint.x, 0, randomPoint.y);

                // Start the ray above the random position
                Vector3 rayStart = randomPosition + hit.normal * placementRadius;
                
                // Calculate the direction, cast a ray back towards the surface
                Vector3 rayDirection = randomPosition - rayStart;
                
                // Raycast to find the surface below the random position
                if (Physics.Raycast(rayStart, rayDirection, out RaycastHit randomHit, Mathf.Infinity, -1, QueryTriggerInteraction.Ignore))
                {
                    // Hit a surface, create the painted prefab
                    CreatePaintedPrefabAt(randomHit);
                }
            }
        }
        
        void CreatePaintedPrefabAt(RaycastHit hit)
        {
            // PrefabUtility is special in that it actually creates the prefab instance in the scene, unlike Instantiate
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(m_Painter.Prefab);
            
            // Set the position of the instance to the hit point
            instance.transform.position = hit.point;
                
            // Check if random scaling is enabled, and apply a random scale if it is
            if (m_EnableRandomScaling.boolValue)
            {
                float minScale = m_MinScale.floatValue;
                float maxScale = m_MaxScale.floatValue;
                float scale = Random.Range(minScale, maxScale);
                instance.transform.localScale = Vector3.one * scale;
            }
            
            // Store the prefab's rotation, then align it with the surface normal
            Quaternion prefabRotation = m_Painter.Prefab.transform.rotation;
            
            // Create a rotation that aligns the prefab's up vector with the surface normal
            Quaternion surfaceAlignedRotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
            
            if (m_EnableRandomRotation.boolValue)
            {
                float yRotation = Random.Range(0, 359f);
                Quaternion randomYRotation = Quaternion.Euler(0, yRotation, 0);
                instance.transform.rotation = randomYRotation * surfaceAlignedRotation * prefabRotation;
            }
            else
            {
                instance.transform.rotation = surfaceAlignedRotation * prefabRotation;
            }
                
            if (m_Painter.Parent)
            {
                // If a parent object is set, make the painted prefab a child of it
                instance.transform.SetParent(m_Painter.Parent.transform, true);
            }
             
            // Register the prefab instance as a newly created object, so it can be undone
            Undo.RegisterCreatedObjectUndo(instance, "Paint Prefab");
        }
    }
}

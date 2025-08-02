using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Dev.RedlineTeam.RHierarchy
{
    /// <summary>
    /// Handles drag and drop operations in the Redline Hierarchy
    /// </summary>
    [InitializeOnLoad]
    public static class RHierarchyDragAndDrop
    {
        private static RHierarchySettings _settings;
        
        /// <summary>
        /// Static constructor
        /// </summary>
        static RHierarchyDragAndDrop()
        {
            _settings = RHierarchySettings.GetOrCreateSettings();
            
            // Register drag and drop handlers
            if (_settings.enableDragAndDropEnhancements)
            {
                EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyItemDragAndDrop;
            }
        }
        
        /// <summary>
        /// Called for each hierarchy item to handle drag and drop
        /// </summary>
        private static void OnHierarchyItemDragAndDrop(int instanceID, Rect selectionRect)
        {
            if (!_settings.enableDragAndDropEnhancements)
                return;

            Event current = Event.current;

            // Only process drag and drop events
            if (current.type != EventType.DragUpdated &&
                current.type != EventType.DragPerform &&
                current.type != EventType.DragExited)
                return;

            // Check if mouse is over this item
            if (!selectionRect.Contains(current.mousePosition))
                return;

            // Get the GameObject for this hierarchy item
            GameObject targetObject = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
            if (targetObject == null)
                return;

            // Only consume the event if a custom handler actually does something
            bool handled = false;
            handled |= HandlePrefabDrag(targetObject, current);
            handled |= HandleComponentDrag(targetObject, current);
            handled |= HandleMaterialDrag(targetObject, current);

            // Only use the event if we handled it
            if (handled)
                current.Use();
        }
        
        /// <summary>
        /// Handle dragging prefabs onto hierarchy items
        /// </summary>
        private static bool HandlePrefabDrag(GameObject targetObject, Event current)
        {
            // Check if we're dragging prefabs
            if (DragAndDrop.objectReferences.Length == 0)
                return false;
                
            bool hasPrefab = false;
            foreach (Object obj in DragAndDrop.objectReferences)
            {
                if (PrefabUtility.GetPrefabAssetType(obj) != PrefabAssetType.NotAPrefab)
                {
                    hasPrefab = true;
                    break;
                }
            }
            
            if (!hasPrefab)
                return false;

            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

            if (current.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                current.Use();
                
                // Instantiate prefabs as children of the target
                foreach (Object obj in DragAndDrop.objectReferences)
                {
                    if (PrefabUtility.GetPrefabAssetType(obj) != PrefabAssetType.NotAPrefab)
                    {
                        GameObject prefabInstance = PrefabUtility.InstantiatePrefab(obj) as GameObject;
                        if (prefabInstance != null)
                        {
                            Undo.RegisterCreatedObjectUndo(prefabInstance, "Instantiate Prefab");
                            prefabInstance.transform.SetParent(targetObject.transform, false);
                        }
                    }
                }
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// Handle dragging components onto hierarchy items
        /// </summary>
        private static bool HandleComponentDrag(GameObject targetObject, Event current)
        {
            // Check if we're dragging MonoScript (component scripts)
            bool hasComponentScript = false;
            foreach (Object obj in DragAndDrop.objectReferences)
            {
                if (obj is MonoScript)
                {
                    hasComponentScript = true;
                    break;
                }
            }
            
            if (!hasComponentScript)
                return false;

            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

            if (current.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                current.Use();
                
                // Add components to the target
                foreach (Object obj in DragAndDrop.objectReferences)
                {
                    if (obj is MonoScript script)
                    {
                        System.Type componentType = script.GetClass();
                        
                        // Check if this is a valid component type
                        if (componentType != null && componentType.IsSubclassOf(typeof(Component)))
                        {
                            // Add the component
                            Undo.AddComponent(targetObject, componentType);
                        }
                    }
                }
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// Handle dragging materials onto hierarchy items
        /// </summary>
        private static bool HandleMaterialDrag(GameObject targetObject, Event current)
        {
            // Check if we're dragging materials
            bool hasMaterial = false;
            foreach (Object obj in DragAndDrop.objectReferences)
            {
                if (obj is Material)
                {
                    hasMaterial = true;
                    break;
                }
            }
            
            // Check if the target has renderers
            Renderer[] renderers = targetObject.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                return false;
                
            if (!hasMaterial)
                return false;

            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

            if (current.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                current.Use();
                
                // Get the first material being dragged
                Material material = null;
                foreach (Object obj in DragAndDrop.objectReferences)
                {
                    if (obj is Material mat)
                    {
                        material = mat;
                        break;
                    }
                }
                
                if (material != null)
                {
                    // Apply material to all renderers
                    foreach (Renderer renderer in renderers)
                    {
                        Undo.RecordObject(renderer, "Apply Material");
                        renderer.sharedMaterial = material;
                    }
                }
                return true;
            }
            return false;
        }
    }
}

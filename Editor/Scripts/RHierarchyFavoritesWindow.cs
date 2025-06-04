using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Dev.RedlineTeam.RHierarchy
{
    /// <summary>
    /// Window for managing favorite GameObjects
    /// </summary>
    public class RHierarchyFavoritesWindow : EditorWindow
    {
        private RHierarchySettings _settings;
        private Vector2 _scrollPosition;
        private string _searchFilter = "";
        private bool _showOnlyActive = false;
        private bool _showOnlyInScene = true;
        private bool _groupByScene = true;
        
        /// <summary>
        /// Open the favorites window
        /// </summary>
        [MenuItem("Redline/Modules/RHierarchy/Favorites Window", false, 1001)]
        public static void OpenWindow()
        {
            RHierarchyFavoritesWindow window = GetWindow<RHierarchyFavoritesWindow>(true, "Favorites", true);
            window.minSize = new Vector2(300, 200);
            window.Show();
        }
        
        private void OnEnable()
        {
            // Load settings
            _settings = RHierarchySettings.GetOrCreateSettings();
            
            // Subscribe to hierarchy changes
            EditorApplication.hierarchyChanged += Repaint;
        }
        
        private void OnDisable()
        {
            // Unsubscribe from hierarchy changes
            EditorApplication.hierarchyChanged -= Repaint;
        }
        
        private void OnGUI()
        {
            if (_settings == null)
            {
                _settings = RHierarchySettings.GetOrCreateSettings();
            }
            
            // Draw toolbar
            DrawToolbar();
            
            // Draw search bar
            EditorGUILayout.Space(5);
            _searchFilter = EditorGUILayout.TextField("Search", _searchFilter);
            
            // Draw filters
            EditorGUILayout.BeginHorizontal();
            _showOnlyActive = EditorGUILayout.ToggleLeft("Active Only", _showOnlyActive, GUILayout.Width(100));
            _showOnlyInScene = EditorGUILayout.ToggleLeft("In Scene Only", _showOnlyInScene, GUILayout.Width(100));
            _groupByScene = EditorGUILayout.ToggleLeft("Group By Scene", _groupByScene, GUILayout.Width(100));
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            // Draw favorites list
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            
            if (_settings.FavoriteInstanceIDs.Count == 0)
            {
                EditorGUILayout.HelpBox("No favorites added yet. Right-click on a GameObject in the hierarchy and select 'Toggle Favorite' to add it.", MessageType.Info);
            }
            else
            {
                DrawFavoritesList();
            }
            
            EditorGUILayout.EndScrollView();
        }
        
        /// <summary>
        /// Draw the toolbar
        /// </summary>
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            if (GUILayout.Button("Clear All", EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                if (EditorUtility.DisplayDialog("Clear Favorites", 
                    "Are you sure you want to remove all favorites?", 
                    "Clear", "Cancel"))
                {
                    _settings.FavoriteInstanceIDs.Clear();
                    EditorUtility.SetDirty(_settings);
                }
            }
            
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                Repaint();
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        /// <summary>
        /// Draw the list of favorites
        /// </summary>
        private void DrawFavoritesList()
        {
            // Get all favorite GameObjects
            var favorites = GetFilteredFavorites();
            
            if (_groupByScene)
            {
                // Group by scene
                var groupedFavorites = favorites.GroupBy(go => go.scene.name);
                
                foreach (var sceneGroup in groupedFavorites)
                {
                    EditorGUILayout.LabelField(sceneGroup.Key, EditorStyles.boldLabel);
                    EditorGUI.indentLevel++;
                    
                    foreach (var gameObject in sceneGroup)
                    {
                        DrawFavoriteItem(gameObject);
                    }
                    
                    EditorGUI.indentLevel--;
                    EditorGUILayout.Space(5);
                }
            }
            else
            {
                // Flat list
                foreach (var gameObject in favorites)
                {
                    DrawFavoriteItem(gameObject);
                }
            }
        }
        
        /// <summary>
        /// Draw a single favorite item
        /// </summary>
        private void DrawFavoriteItem(GameObject gameObject)
        {
            if (gameObject == null)
                return;
                
            EditorGUILayout.BeginHorizontal();
            
            // Draw icon
            Texture2D icon = AssetPreview.GetMiniThumbnail(gameObject);
            if (icon != null)
            {
                GUILayout.Label(icon, GUILayout.Width(16), GUILayout.Height(16));
            }
            
            // Draw name
            if (GUILayout.Button(gameObject.name, EditorStyles.label))
            {
                Selection.activeGameObject = gameObject;
                EditorGUIUtility.PingObject(gameObject);
            }
            
            // Draw active toggle
            bool isActive = gameObject.activeInHierarchy;
            bool newIsActive = EditorGUILayout.Toggle(isActive, GUILayout.Width(20));
            if (newIsActive != isActive)
            {
                Undo.RecordObject(gameObject, "Toggle GameObject Active State");
                gameObject.SetActive(newIsActive);
            }
            
            // Draw remove button
            if (GUILayout.Button("×", GUILayout.Width(20)))
            {
                _settings.RemoveFavorite(gameObject.GetInstanceID());
                EditorUtility.SetDirty(_settings);
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        /// <summary>
        /// Get filtered list of favorite GameObjects
        /// </summary>
        private IEnumerable<GameObject> GetFilteredFavorites()
        {
            var favorites = _settings.FavoriteInstanceIDs
                .Select(id => EditorUtility.InstanceIDToObject(id) as GameObject)
                .Where(go => go != null);
                
            // Apply filters
            if (_showOnlyActive)
            {
                favorites = favorites.Where(go => go.activeInHierarchy);
            }
            
            if (_showOnlyInScene)
            {
                favorites = favorites.Where(go => go.scene.isLoaded);
            }
            
            if (!string.IsNullOrEmpty(_searchFilter))
            {
                favorites = favorites.Where(go => 
                    go.name.IndexOf(_searchFilter, System.StringComparison.OrdinalIgnoreCase) >= 0);
            }
            
            return favorites;
        }
    }
} 
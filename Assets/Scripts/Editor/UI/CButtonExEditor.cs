using System.Collections.Generic;
using System.IO;
using TinyHero.UI;
using UnityEditor;
using UnityEditor.UI;
using UnityEngine;

namespace TinyHero.Tools.Editor
{
    ///<summary>
    /// CButtonEx 인스펙터 확장
    ///</summary>
    [CustomEditor( typeof( CButtonEx ), true )]
    [CanEditMultipleObjects]
    public sealed class CButtonExEditor : ButtonEditor
    {
        private const string SfxSearchRootPath = "Assets/Resources/Audio/SFX";
        private const string DefaultClickSfxClipName = "SFX_CLICK_00";
        private const string NoneOptionLabel = "None";
        private const string UseClickSfxPropertyName = "useClickSfx";
        private const string ClickSfxClipNamePropertyName = "clickSfxClipName";
        private const string ClickSfxVolumeScalePropertyName = "clickSfxVolumeScale";

        ///<summary>
        /// 인스펙터 GUI 렌더링
        ///</summary>
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();
            EditorGUILayout.Space();
            DrawClickSfxSection();
            serializedObject.ApplyModifiedProperties();
        }

        ///<summary>
        /// 클릭 효과음 영역 렌더링
        ///</summary>
        private void DrawClickSfxSection()
        {
            SerializedProperty useClickSfxProperty = serializedObject.FindProperty( UseClickSfxPropertyName );
            SerializedProperty clickSfxClipNameProperty = serializedObject.FindProperty( ClickSfxClipNamePropertyName );
            SerializedProperty clickSfxVolumeScaleProperty = serializedObject.FindProperty( ClickSfxVolumeScalePropertyName );

            if ( useClickSfxProperty == null || clickSfxClipNameProperty == null || clickSfxVolumeScaleProperty == null )
            {
                return;
            }

            EditorGUILayout.LabelField( "효과음", EditorStyles.boldLabel );
            EditorGUILayout.PropertyField( useClickSfxProperty, new GUIContent( "Use Click SFX" ) );

            using ( new EditorGUI.DisabledScope( useClickSfxProperty.boolValue == false ) )
            {
                DrawClickSfxPopup( clickSfxClipNameProperty );
                EditorGUILayout.Slider( clickSfxVolumeScaleProperty, 0.0f, 1.0f, new GUIContent( "Click SFX Volume" ) );
            }
        }

        ///<summary>
        /// 클릭 효과음 선택 팝업 렌더링
        ///</summary>
        private void DrawClickSfxPopup( SerializedProperty _clickSfxClipNameProperty )
        {
            List<string> sfxClipNameList = BuildSfxClipNameList( _clickSfxClipNameProperty.stringValue );
            string currentClipName = string.IsNullOrWhiteSpace( _clickSfxClipNameProperty.stringValue ) ? NoneOptionLabel : _clickSfxClipNameProperty.stringValue.Trim();
            int selectedIndex = ResolveSelectedIndex( sfxClipNameList, currentClipName );
            int nextSelectedIndex = EditorGUILayout.Popup( "Click SFX", selectedIndex, sfxClipNameList.ToArray() );

            if ( nextSelectedIndex < 0 || nextSelectedIndex >= sfxClipNameList.Count )
            {
                return;
            }

            string selectedClipName = sfxClipNameList[ nextSelectedIndex ];
            _clickSfxClipNameProperty.stringValue = string.Equals( selectedClipName, NoneOptionLabel, System.StringComparison.Ordinal ) ? string.Empty : selectedClipName;
        }

        ///<summary>
        /// 효과음 클립 이름 목록 생성
        ///</summary>
        private List<string> BuildSfxClipNameList( string _currentClipName )
        {
            List<string> sfxClipNameList = new List<string>();
            HashSet<string> sfxClipNameSet = new HashSet<string>( System.StringComparer.Ordinal );
            AddSfxClipName( sfxClipNameList, sfxClipNameSet, NoneOptionLabel );
            AddSfxClipName( sfxClipNameList, sfxClipNameSet, DefaultClickSfxClipName );

            if ( AssetDatabase.IsValidFolder( SfxSearchRootPath ) )
            {
                string[] searchRootPathArray = new string[]
                {
                    SfxSearchRootPath
                };
                string[] guidArray = AssetDatabase.FindAssets( "t:AudioClip", searchRootPathArray );

                for ( int index = 0; index < guidArray.Length; index++ )
                {
                    string guid = guidArray[ index ];
                    string assetPath = AssetDatabase.GUIDToAssetPath( guid );
                    string clipName = Path.GetFileNameWithoutExtension( assetPath );
                    AddSfxClipName( sfxClipNameList, sfxClipNameSet, clipName );
                }
            }

            if ( string.IsNullOrWhiteSpace( _currentClipName ) == false )
            {
                AddSfxClipName( sfxClipNameList, sfxClipNameSet, _currentClipName.Trim() );
            }

            return sfxClipNameList;
        }

        ///<summary>
        /// 효과음 클립 이름 추가
        ///</summary>
        private void AddSfxClipName( List<string> _sfxClipNameList, HashSet<string> _sfxClipNameSet, string _clipName )
        {
            if ( _sfxClipNameList == null || _sfxClipNameSet == null || string.IsNullOrWhiteSpace( _clipName ) )
            {
                return;
            }

            if ( _sfxClipNameSet.Contains( _clipName ) )
            {
                return;
            }

            _sfxClipNameSet.Add( _clipName );
            _sfxClipNameList.Add( _clipName );
        }

        ///<summary>
        /// 현재 선택 인덱스 반환
        ///</summary>
        private int ResolveSelectedIndex( List<string> _sfxClipNameList, string _currentClipName )
        {
            if ( _sfxClipNameList == null || _sfxClipNameList.Count == 0 )
            {
                return 0;
            }

            for ( int index = 0; index < _sfxClipNameList.Count; index++ )
            {
                string clipName = _sfxClipNameList[ index ];

                if ( string.Equals( clipName, _currentClipName, System.StringComparison.Ordinal ) )
                {
                    return index;
                }
            }

            return 0;
        }
    }
}

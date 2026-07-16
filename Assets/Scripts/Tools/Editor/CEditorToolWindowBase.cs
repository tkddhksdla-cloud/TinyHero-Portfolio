using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TinyHero.Tools
{
    public enum eEditorAssetOperationResult
    {
        CANCELLED,
        FAILED,
        SUCCESS
    }

    /// <summary>
    /// TinyHero 데이터 편집 창이 공유하는 표시와 에셋 수명주기 기능을 제공하는 기반 클래스입니다.
    /// 개별 창은 데이터 특화 편집과 선택 상태만 담당합니다.
    /// </summary>
    public abstract class CEditorToolWindowBase : EditorWindow
    {
        protected void DrawWindowHeader( string _title, string _description )
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField( _title, EditorStyles.boldLabel );
            EditorGUILayout.HelpBox( _description, MessageType.None );
            EditorGUILayout.Space();
        }

        protected void DrawStatusMessage( string _message, MessageType _messageType )
        {
            EditorGUILayout.HelpBox( _message, _messageType );
        }

        protected bool IsSearchMatch( string _sourceText, string _searchText )
        {
            if ( string.IsNullOrWhiteSpace( _searchText ) )
            {
                return true;
            }

            if ( string.IsNullOrWhiteSpace( _sourceText ) )
            {
                return false;
            }

            string normalizedSearchText = _searchText.Trim();
            bool isMatch = _sourceText.IndexOf( normalizedSearchText, StringComparison.OrdinalIgnoreCase ) >= 0;
            return isMatch;
        }

        protected bool TryDuplicateAsset( UnityEngine.Object _sourceAsset, string _requestedAssetPath, out string _duplicatedAssetPath )
        {
            _duplicatedAssetPath = string.Empty;

            if ( _sourceAsset == null || string.IsNullOrWhiteSpace( _requestedAssetPath ) )
            {
                return false;
            }

            string sourceAssetPath = AssetDatabase.GetAssetPath( _sourceAsset );

            if ( string.IsNullOrWhiteSpace( sourceAssetPath ) )
            {
                return false;
            }

            string duplicatedAssetPath = AssetDatabase.GenerateUniqueAssetPath( _requestedAssetPath );
            bool isCopied = AssetDatabase.CopyAsset( sourceAssetPath, duplicatedAssetPath );

            if ( isCopied == false )
            {
                return false;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            _duplicatedAssetPath = duplicatedAssetPath;
            return true;
        }

        protected eEditorAssetOperationResult TryDeleteAsset( UnityEngine.Object _asset, string _dialogTitle, string _dialogMessage )
        {
            if ( _asset == null )
            {
                return eEditorAssetOperationResult.FAILED;
            }

            string assetPath = AssetDatabase.GetAssetPath( _asset );

            if ( string.IsNullOrWhiteSpace( assetPath ) )
            {
                return eEditorAssetOperationResult.FAILED;
            }

            bool isConfirmed = EditorUtility.DisplayDialog( _dialogTitle, _dialogMessage, "Delete", "Cancel" );

            if ( isConfirmed == false )
            {
                return eEditorAssetOperationResult.CANCELLED;
            }

            bool isDeleted = AssetDatabase.DeleteAsset( assetPath );

            if ( isDeleted == false )
            {
                return eEditorAssetOperationResult.FAILED;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return eEditorAssetOperationResult.SUCCESS;
        }

        protected bool TryGetKeyboardNavigationDirection( out int _direction )
        {
            _direction = 0;
            Event currentEvent = Event.current;

            if ( currentEvent == null || EditorGUIUtility.editingTextField || currentEvent.type != EventType.KeyDown )
            {
                return false;
            }

            if ( currentEvent.keyCode == KeyCode.DownArrow )
            {
                _direction = 1;
            }
            else if ( currentEvent.keyCode == KeyCode.UpArrow )
            {
                _direction = -1;
            }
            else
            {
                return false;
            }

            currentEvent.Use();
            return true;
        }
    }

    /// <summary>
    /// 목록 항목별 검색 대상만 파생 편집기가 정의하도록 분리한 제네릭 기반 클래스입니다.
    /// </summary>
    public abstract class CEditorToolWindowBase<TInfo> : CEditorToolWindowBase
    {
        protected int ResolveNextFilteredIndex( List<TInfo> _filteredInfoList, TInfo _selectedInfo, int _direction )
        {
            if ( _filteredInfoList == null || _filteredInfoList.Count == 0 )
            {
                return -1;
            }

            int selectedIndex = _selectedInfo == null ? -1 : _filteredInfoList.IndexOf( _selectedInfo );
            int currentIndex = selectedIndex < 0 ? 0 : selectedIndex;
            int lastIndex = _filteredInfoList.Count - 1;
            int result = Mathf.Clamp( currentIndex + _direction, 0, lastIndex );
            return result;
        }

        protected int FindInfoIndexByAssetPath( List<TInfo> _infoList, string _assetPath, Func<TInfo, string> _assetPathGetter )
        {
            if ( _infoList == null || string.IsNullOrWhiteSpace( _assetPath ) || _assetPathGetter == null )
            {
                return -1;
            }

            for ( int index = 0; index < _infoList.Count; index++ )
            {
                TInfo info = _infoList[ index ];

                if ( info == null )
                {
                    continue;
                }

                string infoAssetPath = _assetPathGetter.Invoke( info );
                bool isMatched = string.Equals( infoAssetPath, _assetPath, StringComparison.Ordinal );

                if ( isMatched )
                {
                    return index;
                }
            }

            return -1;
        }

        protected List<TInfo> GetFilteredInfoList( List<TInfo> _sourceInfoList, string _searchText )
        {
            List<TInfo> filteredInfoList = new List<TInfo>();

            if ( _sourceInfoList == null )
            {
                return filteredInfoList;
            }

            for ( int index = 0; index < _sourceInfoList.Count; index++ )
            {
                TInfo info = _sourceInfoList[ index ];

                if ( info == null || IsSearchMatch( info, _searchText ) == false )
                {
                    continue;
                }

                filteredInfoList.Add( info );
            }

            return filteredInfoList;
        }

        protected abstract bool IsSearchMatch( TInfo _info, string _searchText );
    }
}

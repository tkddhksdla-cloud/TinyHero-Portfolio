using UnityEditor;
using UnityEngine;

namespace TinyHero.Tools
{
    ///<summary>
    /// 월드 공간 배경 맞춤 에디터 커스텀 에디터
    ///</summary>
    [CustomEditor( typeof( WorldSpaceBackgroundFitter ) )]
    public sealed class WorldSpaceBackgroundFitterEditor : UnityEditor.Editor
    {
        ///<summary>
        /// 활성화 처리
        ///</summary>
        private void OnEnable()
        {
            WorldSpaceBackgroundFitter fitter = target as WorldSpaceBackgroundFitter;

            if ( fitter == null )
            {
                return;
            }

            fitter.SetInspectorRealtimeSyncActive( true );
            EditorApplication.update += HandleEditorUpdate;
        }

        ///<summary>
        /// 비활성화 처리
        ///</summary>
        private void OnDisable()
        {
            EditorApplication.update -= HandleEditorUpdate;
            WorldSpaceBackgroundFitter fitter = target as WorldSpaceBackgroundFitter;

            if ( fitter == null )
            {
                return;
            }

            fitter.SetInspectorRealtimeSyncActive( false );
        }

        ///<summary>
        /// 커스텀 인스펙터 렌더링
        ///</summary>
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space();

            WorldSpaceBackgroundFitter fitter = ( WorldSpaceBackgroundFitter )target;
            bool hasSpriteRenderer = fitter.GetComponent<SpriteRenderer>() != null;

            if ( hasSpriteRenderer == false )
            {
                EditorGUILayout.HelpBox( "媛숈? ?ㅻ툕?앺듃??SpriteRenderer媛 ?꾩슂?⑸땲??", MessageType.Warning );
            }

            using ( new EditorGUI.DisabledScope( hasSpriteRenderer == false ) )
            {
                if ( GUILayout.Button( "Capture Base Scale" ) )
                {
                    ApplyCaptureBaseScale( fitter );
                }

                if ( GUILayout.Button( "Apply Background Fit" ) )
                {
                    ApplyBackgroundFit( fitter );
                }
            }
        }

        ///<summary>
        /// 에디터 갱신 처리
        ///</summary>
        private void HandleEditorUpdate()
        {
            WorldSpaceBackgroundFitter fitter = target as WorldSpaceBackgroundFitter;

            if ( fitter == null )
            {
                EditorApplication.update -= HandleEditorUpdate;
                return;
            }

            fitter.ApplyFitIfSpriteChanged();
        }

        ///<summary>
        /// 기준 스케일 저장
        ///</summary>
        private void ApplyCaptureBaseScale(WorldSpaceBackgroundFitter _fitter)
        {
            Undo.RecordObject( _fitter.transform, "Capture Background Base Scale" );
            Undo.RecordObject( _fitter, "Capture Background Base Scale" );
            _fitter.CaptureBaseScale();
            EditorUtility.SetDirty( _fitter );
            EditorUtility.SetDirty( _fitter.transform );
        }

        ///<summary>
        /// 배경 맞춤 적용
        ///</summary>
        private void ApplyBackgroundFit(WorldSpaceBackgroundFitter _fitter)
        {
            Undo.RecordObject( _fitter.transform, "Apply Background Fit" );
            _fitter.ApplyFit();
            EditorUtility.SetDirty( _fitter.transform );
        }
    }
}



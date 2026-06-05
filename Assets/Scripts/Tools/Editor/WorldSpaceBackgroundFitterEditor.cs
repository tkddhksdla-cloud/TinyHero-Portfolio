using UnityEditor;
using UnityEngine;

namespace TinyHero.Tools
{
    /// <summary>
    /// 월드 배경 맞춤 컴포넌트의 작업 버튼과 실시간 감시를 제공한다.
    /// </summary>
    [CustomEditor( typeof( WorldSpaceBackgroundFitter ) )]
    public sealed class WorldSpaceBackgroundFitterEditor : Editor
    {
        /// <summary>
        /// 인스펙터 활성화 시 실시간 스프라이트 감시를 시작한다.
        /// </summary>
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

        /// <summary>
        /// 인스펙터 비활성화 시 실시간 스프라이트 감시를 중지한다.
        /// </summary>
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

        /// <summary>
        /// 인스펙터와 배경 맞춤 버튼을 그린다.
        /// </summary>
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space();

            WorldSpaceBackgroundFitter fitter = ( WorldSpaceBackgroundFitter )target;
            bool hasSpriteRenderer = fitter.GetComponent<SpriteRenderer>() != null;

            if ( hasSpriteRenderer == false )
            {
                EditorGUILayout.HelpBox( "같은 오브젝트에 SpriteRenderer가 필요합니다.", MessageType.Warning );
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

        /// <summary>
        /// 인스펙터가 열려 있는 동안 스프라이트 변경을 감시한다.
        /// </summary>
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

        /// <summary>
        /// 현재 스케일을 기준 스케일로 저장한다.
        /// </summary>
        private void ApplyCaptureBaseScale( WorldSpaceBackgroundFitter fitter )
        {
            Undo.RecordObject( fitter.transform, "Capture Background Base Scale" );
            Undo.RecordObject( fitter, "Capture Background Base Scale" );
            fitter.CaptureBaseScale();
            EditorUtility.SetDirty( fitter );
            EditorUtility.SetDirty( fitter.transform );
        }

        /// <summary>
        /// 배경을 카메라 화면 크기에 맞게 갱신한다.
        /// </summary>
        private void ApplyBackgroundFit( WorldSpaceBackgroundFitter fitter )
        {
            Undo.RecordObject( fitter.transform, "Apply Background Fit" );
            fitter.ApplyFit();
            EditorUtility.SetDirty( fitter.transform );
        }
    }
}

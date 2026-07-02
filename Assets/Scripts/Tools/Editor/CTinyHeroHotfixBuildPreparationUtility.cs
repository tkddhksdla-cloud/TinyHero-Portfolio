using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace TinyHero.Tools
{
    ///<summary>
    /// Hotfix 빌드 준비 통합 유틸리티
    ///</summary>
    public static class CTinyHeroHotfixBuildPreparationUtility
    {
        private const string MenuPath = "TinyHero/Build/Prepare Hotfix Build";

        private sealed class CPreparationStepResult
        {
            public string stepName;
            public bool isPassed;
            public string detailMessage;

            ///<summary>
            /// 빌드 준비 단계 결과 초기화
            ///</summary>
            public CPreparationStepResult( string _stepName, bool _isPassed, string _detailMessage )
            {
                stepName = string.IsNullOrWhiteSpace( _stepName ) ? string.Empty : _stepName.Trim();
                isPassed = _isPassed;
                detailMessage = string.IsNullOrWhiteSpace( _detailMessage ) ? string.Empty : _detailMessage.Trim();
            }
        }

        ///<summary>
        /// Hotfix 빌드 준비 메뉴 실행
        ///</summary>
        [MenuItem( MenuPath )]
        public static void PrepareHotfixBuildFromMenu()
        {
            bool isPrepared = PrepareHotfixBuild();

            if ( isPrepared )
            {
                Debug.Log( "[TinyHero Build] Prepare Hotfix Build completed." );
            }
        }

        ///<summary>
        /// Hotfix 빌드 준비 실행
        ///</summary>
        public static bool PrepareHotfixBuild()
        {
            bool result = PrepareHotfixBuild( true );
            return result;
        }

        ///<summary>
        /// Hotfix 빌드 준비 실행
        ///</summary>
        public static bool PrepareHotfixBuild( bool _clearConsoleBeforeRun )
        {
            if ( _clearConsoleBeforeRun )
            {
                ClearConsole();
            }

            List<CPreparationStepResult> stepResultList = new List<CPreparationStepResult>();
            List<string> issueList = new List<string>();
            bool isPayloadSynced = ExecuteHotfixPayloadSync( stepResultList, issueList );
            bool isAddressablesSynced = false;
            bool isHotfixReady = false;
            bool isPrebuildReady = false;

            if ( isPayloadSynced )
            {
                isAddressablesSynced = ExecuteAddressablesSync( stepResultList, issueList );
            }
            else
            {
                AddSkippedStep( stepResultList, "Addressables Sync", "Hotfix Payload Sync 실패로 인해 건너뜀" );
            }

            if ( isAddressablesSynced )
            {
                isHotfixReady = ExecuteHotfixReadinessValidation( stepResultList, issueList );
            }
            else
            {
                AddSkippedStep( stepResultList, "Hotfix Readiness Validation", "Addressables Sync 실패로 인해 건너뜀" );
            }

            if ( isHotfixReady )
            {
                isPrebuildReady = ExecutePrebuildReadinessValidation( stepResultList, issueList, _clearConsoleBeforeRun );
            }
            else
            {
                AddSkippedStep( stepResultList, "Prebuild Readiness Validation", "Hotfix Readiness Validation 실패로 인해 건너뜀" );
            }

            bool result = isPayloadSynced && isAddressablesSynced && isHotfixReady && isPrebuildReady && issueList.Count == 0;
            ReportPreparationResult( result, stepResultList, issueList );
            return result;
        }

        ///<summary>
        /// Hotfix 페이로드 동기화 단계 실행
        ///</summary>
        private static bool ExecuteHotfixPayloadSync( List<CPreparationStepResult> _stepResultList, List<string> _issueList )
        {
            List<string> payloadIssueList = new List<string>();
            bool isSynced = CTinyHeroHotfixPayloadUtility.TrySyncHotfixPayload( payloadIssueList );
            AppendIssues( _issueList, "Hotfix Payload Sync", payloadIssueList );
            string detailMessage = isSynced ? "TinyHero.Hotfix.dll.bytes 갱신 완료" : BuildIssueSummary( payloadIssueList );
            AddStepResult( _stepResultList, "Hotfix Payload Sync", isSynced, detailMessage );
            return isSynced;
        }

        ///<summary>
        /// Addressables 동기화 단계 실행
        ///</summary>
        private static bool ExecuteAddressablesSync( List<CPreparationStepResult> _stepResultList, List<string> _issueList )
        {
            List<string> addressablesIssueList = new List<string>();
            bool isSynced = CTinyHeroAddressablesSyncUtility.TrySyncRuntimeResources( addressablesIssueList, out int registeredCount );
            AppendIssues( _issueList, "Addressables Sync", addressablesIssueList );
            string detailMessage = isSynced ? $"Runtime Resources 동기화 완료. Count: {registeredCount}" : BuildIssueSummary( addressablesIssueList );
            AddStepResult( _stepResultList, "Addressables Sync", isSynced, detailMessage );
            return isSynced;
        }

        ///<summary>
        /// Hotfix 준비 상태 검증 단계 실행
        ///</summary>
        private static bool ExecuteHotfixReadinessValidation( List<CPreparationStepResult> _stepResultList, List<string> _issueList )
        {
            List<string> readinessIssueList = new List<string>();
            bool isReady = CTinyHeroHotfixReadinessValidator.ValidateHotfixReadiness( readinessIssueList );
            AppendIssues( _issueList, "Hotfix Readiness Validation", readinessIssueList );
            string detailMessage = isReady ? "HybridCLR/Hotfix 설정 검증 완료" : BuildIssueSummary( readinessIssueList );
            AddStepResult( _stepResultList, "Hotfix Readiness Validation", isReady, detailMessage );
            return isReady;
        }

        ///<summary>
        /// 빌드 전 준비 상태 검증 단계 실행
        ///</summary>
        private static bool ExecutePrebuildReadinessValidation( List<CPreparationStepResult> _stepResultList, List<string> _issueList, bool _clearConsoleBeforeRun )
        {
            if ( _clearConsoleBeforeRun )
            {
                ClearConsole();
            }

            bool isReady = CTinyHeroPrebuildReadinessValidator.ValidatePrebuildReadiness();
            string detailMessage = isReady ? "Prebuild Readiness 검증 완료" : "Prebuild Readiness 검증 실패. Console의 [TinyHero Build] 로그 확인";

            if ( isReady == false )
            {
                _issueList.Add( "Prebuild Readiness Validation: 세부 실패 원인은 Console의 [TinyHero Build] Error/Warning 확인" );
            }

            AddStepResult( _stepResultList, "Prebuild Readiness Validation", isReady, detailMessage );
            return isReady;
        }

        ///<summary>
        /// 단계 결과 추가
        ///</summary>
        private static void AddStepResult( List<CPreparationStepResult> _stepResultList, string _stepName, bool _isPassed, string _detailMessage )
        {
            if ( _stepResultList == null )
            {
                return;
            }

            CPreparationStepResult stepResult = new CPreparationStepResult( _stepName, _isPassed, _detailMessage );
            _stepResultList.Add( stepResult );
        }

        ///<summary>
        /// 건너뛴 단계 결과 추가
        ///</summary>
        private static void AddSkippedStep( List<CPreparationStepResult> _stepResultList, string _stepName, string _detailMessage )
        {
            AddStepResult( _stepResultList, _stepName, false, _detailMessage );
        }

        ///<summary>
        /// 단계별 이슈 목록 병합
        ///</summary>
        private static void AppendIssues( List<string> _targetIssueList, string _stepName, List<string> _sourceIssueList )
        {
            if ( _targetIssueList == null || _sourceIssueList == null )
            {
                return;
            }

            for ( int index = 0; index < _sourceIssueList.Count; index++ )
            {
                string issue = _sourceIssueList[ index ];
                _targetIssueList.Add( $"{_stepName}: {issue}" );
            }
        }

        ///<summary>
        /// 이슈 요약 문자열 구성
        ///</summary>
        private static string BuildIssueSummary( List<string> _issueList )
        {
            if ( _issueList == null || _issueList.Count == 0 )
            {
                return "알 수 없는 실패";
            }

            string result = _issueList[ 0 ];
            return result;
        }

        ///<summary>
        /// Hotfix 빌드 준비 결과 출력
        ///</summary>
        private static void ReportPreparationResult( bool _isPrepared, List<CPreparationStepResult> _stepResultList, List<string> _issueList )
        {
            if ( _isPrepared )
            {
                LogStepResults( _stepResultList );
                return;
            }

            LogStepResults( _stepResultList );
            LogIssues( _issueList );
        }

        ///<summary>
        /// 단계별 결과 로그 출력
        ///</summary>
        private static void LogStepResults( List<CPreparationStepResult> _stepResultList )
        {
            if ( _stepResultList == null )
            {
                return;
            }

            for ( int index = 0; index < _stepResultList.Count; index++ )
            {
                CPreparationStepResult stepResult = _stepResultList[ index ];
                string statusText = stepResult.isPassed ? "PASS" : "FAIL";
                Debug.Log( $"[TinyHero Build] [{statusText}] {stepResult.stepName} - {stepResult.detailMessage}" );
            }
        }

        ///<summary>
        /// 이슈 목록 에러 출력
        ///</summary>
        private static void LogIssues( List<string> _issueList )
        {
            if ( _issueList == null )
            {
                return;
            }

            for ( int index = 0; index < _issueList.Count; index++ )
            {
                string issue = _issueList[ index ];
                Debug.LogError( $"[TinyHero Build] Prepare Hotfix Build failed. {issue}" );
            }
        }

        ///<summary>
        /// Unity 콘솔 로그 정리
        ///</summary>
        private static void ClearConsole()
        {
            System.Type logEntriesType = typeof( EditorWindow ).Assembly.GetType( "UnityEditor.LogEntries" );

            if ( logEntriesType == null )
            {
                return;
            }

            MethodInfo clearMethod = logEntriesType.GetMethod( "Clear", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic );

            if ( clearMethod == null )
            {
                return;
            }

            clearMethod.Invoke( null, null );
        }
    }
}

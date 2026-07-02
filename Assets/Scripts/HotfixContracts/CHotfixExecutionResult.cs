namespace TinyHero.HotfixContracts
{
    ///<summary>
    /// Hotfix 실행 결과 데이터
    ///</summary>
    public sealed class CHotfixExecutionResult
    {
        private readonly eHotfixExecutionStatus status;
        private readonly string message;

        ///<summary>Hotfix 실행 결과 생성</summary>
        public CHotfixExecutionResult( eHotfixExecutionStatus _status, string _message )
        {
            status = _status;
            message = _message != null ? _message : string.Empty;
        }

        ///<summary>성공 결과 생성</summary>
        public static CHotfixExecutionResult CreateSuccess( string _message )
        {
            CHotfixExecutionResult result = new CHotfixExecutionResult( eHotfixExecutionStatus.SUCCESS, _message );
            return result;
        }

        ///<summary>차단 결과 생성</summary>
        public static CHotfixExecutionResult CreateBlocked( string _message )
        {
            CHotfixExecutionResult result = new CHotfixExecutionResult( eHotfixExecutionStatus.BLOCKED, _message );
            return result;
        }

        ///<summary>실패 결과 생성</summary>
        public static CHotfixExecutionResult CreateFailed( string _message )
        {
            CHotfixExecutionResult result = new CHotfixExecutionResult( eHotfixExecutionStatus.FAILED, _message );
            return result;
        }

        ///<summary>Fallback 결과 생성</summary>
        public static CHotfixExecutionResult CreateFallback( string _message )
        {
            CHotfixExecutionResult result = new CHotfixExecutionResult( eHotfixExecutionStatus.FALLBACK, _message );
            return result;
        }

        ///<summary>Hotfix 실행 상태 반환</summary>
        public eHotfixExecutionStatus GetStatus()
        {
            eHotfixExecutionStatus result = status;
            return result;
        }

        ///<summary>Hotfix 실행 메시지 반환</summary>
        public string GetMessage()
        {
            string result = message;
            return result;
        }

        ///<summary>Hotfix 실행 성공 여부 반환</summary>
        public bool IsSuccess()
        {
            bool result = status == eHotfixExecutionStatus.SUCCESS;
            return result;
        }
    }
}

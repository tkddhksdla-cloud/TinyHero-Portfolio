using UnityEngine;

namespace TinyHero.Skill
{
    ///<summary>
    /// 스킬 툴 범위 미리보기 데이터
    ///</summary>
    public struct CSkillToolRangePreviewData
    {
        public bool isValid;
        public eSkillToolRangePreviewShape shapeType;
        public Vector3 worldCenterPosition;
        public float radius;
    }
}

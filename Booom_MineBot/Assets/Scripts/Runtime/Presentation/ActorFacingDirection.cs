namespace Minebot.Presentation
{
    /// <summary>
    /// 角色朝向方向，用于帧动画的4方向配置
    /// </summary>
    public enum ActorFacingDirection
    {
        /// <summary>朝前</summary>
        Front,
        
        /// <summary>朝后</summary>
        Back,
        
        /// <summary>朝侧方（朝左时自动水平翻转）</summary>
        Side
    }
}

namespace GeoModel.AudioSystem
{
    public static class AudioKeys
    {
        public static class BGM
        {
            public const string Field = "bgm_field";
            public const string Lab   = "bgm_lab";
            public const string Story = "bgm_story";
        }

        public static class SFX
        {
            public const string DrillLoop     = "sfx_drill_loop";
            public const string DrillComplete = "sfx_drill_complete";
            public const string HammerHit     = "sfx_hammer_hit";
            public const string SampleSpawn   = "sfx_sample_spawn";
            public const string SampleDrop    = "sfx_sample_drop";
            public const string SampleBounce  = "sfx_sample_bounce";
            public const string SamplePickup  = "sfx_sample_pickup";
            public const string ToolPlace     = "sfx_tool_place";
            public const string SceneSwitch   = "sfx_scene_switch";
            public const string SceneTeleport = "sfx_scene_teleport";
            public const string DroneLoop     = "sfx_drone_loop";
            public const string DrillCarLoop  = "sfx_drillcar_loop";
            // 与知识幻灯片共用的素材（Resources/Audio/SFX 下按文件名加载）
            public const string RockBreak     = "slide_roll";   // 石头滚落：岩块被敲下来
            public const string RockCrumble   = "slide_sand";   // 碎石簌簌落下：钻进时的岩屑
            public const string MetalScrape   = "slide_scrape"; // 金属摩擦：架设三脚架、提钻

            public static readonly string[] Footsteps =
            {
                "sfx_footstep_01",
                "sfx_footstep_02",
                "sfx_footstep_03",
                "sfx_footstep_04"
            };
        }

        public static class UI
        {
            public const string TabOpen    = "sfx_ui_tab_open";
            public const string TabClose   = "sfx_ui_tab_close";
            public const string Click      = "sfx_ui_click";
            public const string Hover      = "sfx_ui_hover";
            public const string PanelOpen  = "sfx_ui_panel_open";
            public const string PanelClose = "sfx_ui_panel_close";
        }
    }
}

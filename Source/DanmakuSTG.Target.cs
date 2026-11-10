using UnrealBuildTool;

public class DanmakuSTGTarget : TargetRules
{
	public DanmakuSTGTarget(TargetInfo Target) : base(Target)
	{
		Type = TargetType.Game;
		DefaultBuildSettings = BuildSettingsVersion.Latest;
		IncludeOrderVersion = EngineIncludeOrderVersion.Latest;
		ExtraModuleNames.Add("DanmakuSTG");
	}
}

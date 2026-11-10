using UnrealBuildTool;

public class DanmakuSTGEditorTarget : TargetRules
{
	public DanmakuSTGEditorTarget(TargetInfo Target) : base(Target)
	{
		Type = TargetType.Editor;
		DefaultBuildSettings = BuildSettingsVersion.Latest;
		IncludeOrderVersion = EngineIncludeOrderVersion.Latest;
		ExtraModuleNames.Add("DanmakuSTG");
	}
}

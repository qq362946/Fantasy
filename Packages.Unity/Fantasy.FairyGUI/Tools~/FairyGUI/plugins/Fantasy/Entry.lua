--- 点击发布工程时的回调
---@param handler CS.FairyEditor.PublishHandler 发布处理者
function onPublish(handler)
    -- 阻止编辑器默认生成代码的逻辑.
    handler.genCode = false
    -- 将中文转换为拼音，删除特殊字符等.
    local codePkgName = handler:ToFilename(handler.pkg.name);
    --local settings = handler.project:GetSettings("Publish").codeGeneration
    --handler.project:GetSettings("Publish").path = PluginPath .. "/../../../../Fantasy/Assets/Bundles/UI/" .. codePkgName
    -- 生成UI代码的路径，如果发布失败仔细检查下路径是否正确
    local exportCodePath = PluginPath .. '/../../../../Unity/Assets/Scripts/Fantasy/Fantasy.Model/Generate/UI/' .. codePkgName
    handler.exportCodePath = exportCodePath
    local classes = handler:CollectClasses(true, true, nil)
    -- 检查目标文件夹是否存在，并删除旧文件.
    handler:SetupCodeFolder(exportCodePath .. '/', "cs")
    local classCnt = classes.Count
    local writer = CodeWriter.new()
    local getMemberByName = true
    
    for i = 0,classCnt - 1 do
        local classInfo = classes[i]
        writer:reset()
        
        writer:writeln('using FairyGUI;')
        writer:writeln('using FairyGUI.Utils;')
        writer:writeln('using Fantasy;')
        writer:writeln()
        writer:writeln('namespace Fantasy')
        writer:startBlock()
        writer:writeln('public partial class %s : FairyUI', classInfo.className)
        writer:startBlock()
        
        writer:writeln('public override string PackageName => "%s";', codePkgName)
        writer:writeln('public override string ComponentName => "%s";', classInfo.resName)
        writer:writeln('public override string ConfigBundleName => "%s";', string.lower(codePkgName))
        writer:writeln('public override string ResourceBundleName => "%s";', string.lower(codePkgName))
        writer:writeln()
        
        local members = classInfo.members
        local memberCnt = members.Count

        for j = 0,memberCnt - 1 do
            local memberInfo = members[j]
            writer:writeln('public %s %s;', memberInfo.type, memberInfo.varName)
        end

        writer:writeln('public const string URL = "ui://%s%s";', handler.pkg.id, classInfo.resId)
        writer:writeln()

        writer:writeln('public override void OnCreate()')
        writer:startBlock()
        for j = 0,memberCnt - 1 do
            local memberInfo = members[j]
            if memberInfo.group == 0 then
                if getMemberByName then
                    writer:writeln('%s = (%s)GComponent.GetChild("%s");', memberInfo.varName, memberInfo.type, memberInfo.name)
                else
                    writer:writeln('%s = (%s)GComponent.GetChildAt(%s);', memberInfo.varName, memberInfo.type, memberInfo.index)
                end
            elseif memberInfo.group == 1 then
                if getMemberByName then
                    writer:writeln('%s = GComponent.GetController("%s");', memberInfo.varName, memberInfo.name)
                else
                    writer:writeln('%s = GComponent.GetControllerAt(%s);', memberInfo.varName, memberInfo.index)
                end
            else
                if getMemberByName then
                    writer:writeln('%s = GComponent.GetTransition("%s");', memberInfo.varName, memberInfo.name)
                else
                    writer:writeln('%s = GComponent.GetTransitionAt(%s);', memberInfo.varName, memberInfo.index)
                end
            end
        end
        
        writer:endBlock()
        writer:endBlock()
        writer:endBlock()
        writer:save(exportCodePath..'/'..classInfo.className..'.cs')
    end
end
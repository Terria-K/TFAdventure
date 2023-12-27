---@diagnostic disable-next-line: undefined-field
local luanet = _G.luanet;

local import_type, load_assembly = luanet.import_type, luanet.load_assembly


local mt = {
    __index = function(package, classname)
        local class = rawget(package, classname)
        if class == nil then
            class = import_type(package.packageName .. "." .. classname)
            if class == nil then class = import_type(classname) end
            package[classname] = class
        end
        return class
    end
}


local function import(ns)
    local t = { packageName = ns }
    setmetatable(t, mt)
    return t
end




local tree
local function scriptLoad(name)
    local err, packed = pcall(tree, name)
    if not err then
        return packed
    end

    local path = packed[0]
    local script = packed[1]

    path = path or name

    if not script then
        return "Lua script not found: " .. name
    end

    return load(script, path, "t")
end

table.insert(package.searchers, scriptLoad)

local mtNamespace = {}

local function loadSupportedFunction(name)
    local sharpName = name
    local object = mtNamespace[sharpName]
    if not object then
        return "This object or lua script: " .. name .. "Cannot be found"
    end
    return function() return object end
end

local function addToImport(luaName, csName)
    mtNamespace[luaName] = import(csName)
end

table.insert(package.searchers, loadSupportedFunction)

local function load(resourceTree)
    tree = resourceTree
end


return scriptLoad, load_assembly, addToImport, load
--[[
	This script automatically creates the symlinks needed from your Garry's Mod install to Source.NET
	It should be cross platform and work with any version of Lua 5.1 or higher (and JIT).
]]

local isWindows = package.config:sub(1, 1) == '\\'
local isUnix = not isWindows

local isLinux = false
if isUnix then
	local handle = assert(io.popen("uname -s", "r"), "Failed to run uname")
	local uname = handle:read("*l")
	handle:close()

	isLinux = uname == "Linux"
end

if not (isWindows or isLinux) then
	error("Only Windows and Linux are supported.")
end

---@param path string
local function exists(path)
	local ok, err, code = os.rename(path, path)

	if not ok and code == 13 then
		-- Permission denied, but it exists
		return true
	end

	return ok, err
end

---@param path string
local function isdir(path)
	return exists(path .. "/")
end

local mklink ---@type fun(target: string, linkPath: string): boolean, string?
if isWindows then
	-- TODO: implement mklink on windows
elseif isLinux then
	function mklink(target, linkPath)
		local command = string.format("ln -s %q %q", target, linkPath)
		return os.execute(command) == 0
	end
end

local getSteamDir ---@type fun(): string?
if isWindows then
	error("getsteamdir unimplemented on windows, search registry and do stuff")
elseif isLinux then
	function getSteamDir()
		local home = os.getenv("HOME")

		do
			local nativePath = home .. "/.local/share/Steam"
			if isdir(nativePath) then
				return nativePath
			end
		end

		-- TODO: support flatpak
	end
end

---@param content string
---@return table<string, unknown>
local function parseVDF(content)
	local ptr = 1

	---@param pattern string
	local function consume(pattern) ---@return string?
		local start, finish, match = string.find(content, pattern, ptr)
		if start then
			ptr = finish + 1
			return match or true
		end
	end

	local function whitespace()
		return consume("^%s+")
	end

	local function string()
		whitespace()
		return consume('^"([^"]*)"')
	end

	local value
	local function object()
		whitespace()
		if not consume("^{") then return end

		local obj = {}
		while true do
			whitespace()

			if consume("^}") then
				break
			end

			local key = assert(string(), "Expected string key in object at position " .. ptr)
			local val = assert(value(), "Expected value in object at position " .. ptr)

			obj[key] = val
		end

		return obj
	end

	function value()
		return string() or object()
	end

	local key = assert(string(), "Expected root key at position " .. ptr)
	local obj = assert(object(), "Expected root object at position " .. ptr)

	return { [key] = obj }
end

---@param steamDir string
local function getGmodDir(steamDir)
	local libraryFoldersPath = steamDir .. "/steamapps/libraryfolders.vdf"
	local handle = assert(io.open(libraryFoldersPath, "rb"), "Failed to open libraryfolders.vdf")
	local content = handle:read("*a")
	handle:close()

	---@type { libraryfolders: table<string, { apps: table<string, string>, path: string }> }
	local vdf = parseVDF(content)

	for _, folder in pairs(vdf.libraryfolders) do
		if folder.apps["4000"] then
			return folder.path .. "/steamapps/common/GarrysMod"
		end
	end
end

local steamDir = getSteamDir()
if not steamDir then
	error("Could not find Steam directory.")
end

local gmodDir = getGmodDir(steamDir)
if not gmodDir then
	error("Could not find Garry's Mod directory.")
end

local links = {
	-- Garry's Mod content
	["hl2/garrysmod_dir.vpk"] = gmodDir .. "/garrysmod/garrysmod_dir.vpk",
	["hl2/garrysmod_000.vpk"] = gmodDir .. "/garrysmod/garrysmod_000.vpk",
	["hl2/garrysmod_001.vpk"] = gmodDir .. "/garrysmod/garrysmod_001.vpk",
	["hl2/garrysmod_002.vpk"] = gmodDir .. "/garrysmod/garrysmod_002.vpk",

	-- HL2 content
	["hl2/content_hl2_dir.vpk"] = gmodDir .. "/sourceengine/content_hl2_dir.vpk",
	["hl2/content_hl2_000.vpk"] = gmodDir .. "/sourceengine/content_hl2_000.vpk",
	["hl2/content_hl2_001.vpk"] = gmodDir .. "/sourceengine/content_hl2_001.vpk",
	["hl2/content_hl2_002.vpk"] = gmodDir .. "/sourceengine/content_hl2_002.vpk",
	["hl2/content_hl2_003.vpk"] = gmodDir .. "/sourceengine/content_hl2_003.vpk",
	["hl2/content_hl2_004.vpk"] = gmodDir .. "/sourceengine/content_hl2_004.vpk",
	["hl2/content_hl2_005.vpk"] = gmodDir .. "/sourceengine/content_hl2_005.vpk",
	["hl2/content_hl2_006.vpk"] = gmodDir .. "/sourceengine/content_hl2_006.vpk",

	["hl2/hl2_misc_dir.vpk"] = gmodDir .. "/sourceengine/hl2_misc_dir.vpk",
	["hl2/hl2_misc_000.vpk"] = gmodDir .. "/sourceengine/hl2_misc_000.vpk",
	["hl2/hl2_misc_001.vpk"] = gmodDir .. "/sourceengine/hl2_misc_001.vpk",
	["hl2/hl2_misc_002.vpk"] = gmodDir .. "/sourceengine/hl2_misc_002.vpk",
	["hl2/hl2_misc_003.vpk"] = gmodDir .. "/sourceengine/hl2_misc_003.vpk",

	["hl2/hl2_sound_misc_dir.vpk"] = gmodDir .. "/sourceengine/hl2_sound_misc_dir.vpk",
	["hl2/hl2_sound_misc_000.vpk"] = gmodDir .. "/sourceengine/hl2_sound_misc_000.vpk",
	["hl2/hl2_sound_misc_001.vpk"] = gmodDir .. "/sourceengine/hl2_sound_misc_001.vpk",
	["hl2/hl2_sound_misc_002.vpk"] = gmodDir .. "/sourceengine/hl2_sound_misc_002.vpk",

	["hl2/hl2_textures_dir.vpk"] = gmodDir .. "/sourceengine/hl2_textures_dir.vpk",
	["hl2/hl2_textures_000.vpk"] = gmodDir .. "/sourceengine/hl2_textures_000.vpk",
	["hl2/hl2_textures_001.vpk"] = gmodDir .. "/sourceengine/hl2_textures_001.vpk",
	["hl2/hl2_textures_002.vpk"] = gmodDir .. "/sourceengine/hl2_textures_002.vpk",
	["hl2/hl2_textures_003.vpk"] = gmodDir .. "/sourceengine/hl2_textures_003.vpk",
	["hl2/hl2_textures_004.vpk"] = gmodDir .. "/sourceengine/hl2_textures_004.vpk",
	["hl2/hl2_textures_005.vpk"] = gmodDir .. "/sourceengine/hl2_textures_005.vpk",
	["hl2/hl2_textures_006.vpk"] = gmodDir .. "/sourceengine/hl2_textures_006.vpk",
	["hl2/hl2_textures_007.vpk"] = gmodDir .. "/sourceengine/hl2_textures_007.vpk",
	["hl2/hl2_textures_008.vpk"] = gmodDir .. "/sourceengine/hl2_textures_008.vpk",
	["hl2/hl2_textures_009.vpk"] = gmodDir .. "/sourceengine/hl2_textures_009.vpk",
	["hl2/hl2_textures_010.vpk"] = gmodDir .. "/sourceengine/hl2_textures_010.vpk",
}

local scriptPath = debug.getinfo(1, "S").source:sub(2)
local gameAssetsDir = scriptPath:match("^(.*)/symlink.lua$")
if not gameAssetsDir then
	error("Could not determine Game.Assets directory.")
end

for linkPath, targetPath in pairs(links) do
	local absLinkPath = gameAssetsDir .. "/" .. linkPath

	if not exists(absLinkPath) then
		assert(mklink(targetPath, absLinkPath), "Failed to create symlink for " .. absLinkPath)
	end
end

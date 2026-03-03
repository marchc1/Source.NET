global using static Game.Server.MapEntities;

using Game.Shared;

using Source;
using Source.Common;
using Source.Common.Engine;

namespace Game.Server;

struct HierarchicalSpawn_t
{
	public BaseEntity? Entity;
	public int Depth;
	public BaseEntity DeferredParent;     // attachment parents can't be set until the parents are spawned
	public string DeferredParentAttachment; // so defer setting them up until the second pass
};

struct HierarchicalSpawnMapData_t
{
	public string MapData;
	public int MapDataLength;
};

public interface IMapEntityFilter
{
	bool ShouldCreateEntity(ReadOnlySpan<char> className);
	BaseEntity? CreateNextEntity(ReadOnlySpan<char> className);
}

public class PointTemplate : BaseEntity { } // TODO move this

public static class MapEntities
{
	static ref Edict? g_pForceAttachEdict => ref BaseEntity.g_pForceAttachEdict;

	public static BaseEntity? CreateEntityByName(ReadOnlySpan<char> className, int forceEdictIndex = -1) {
		if (forceEdictIndex != -1) {
			g_pForceAttachEdict = engine.CreateEdict(forceEdictIndex);
			if (g_pForceAttachEdict == null)
				Error($"CreateEntityByName( {className}, {forceEdictIndex} ) - CreateEdict failed.");
		}

		IServerNetworkable? network = EntityFactoryDictionary().Create(className);
		g_pForceAttachEdict = null;

		if (network == null)
			return null;

		BaseEntity? entity = (BaseEntity?)network.GetBaseEntity();
		Assert(entity);
		return entity;
	}

	public static void ParseAllEntities(ReadOnlySpan<char> mapData, IMapEntityFilter? filter = null, bool activateEntities = false) {
		HierarchicalSpawnMapData_t[] pSpawnMapData = new HierarchicalSpawnMapData_t[Constants.NUM_ENT_ENTRIES];
		HierarchicalSpawn_t[] spawnList = new HierarchicalSpawn_t[Constants.NUM_ENT_ENTRIES];

		List<PointTemplate> pointTemplates = [];
		int numEntities = 0;

		Span<char> tokenBuffer = new char[EntityMapData.MAPKEY_MAXLENGTH];

		// Allow the tools to spawn different things
		// if (serverenginetools) { todo?
		// 	mapData = serverenginetools.GetEntityData(mapData);
		// }

		//  Loop through all entities in the map data, creating each.
		for (; true; mapData = MapEntity.SkipToNextEntity(mapData, tokenBuffer)) {
			// Parse the opening brace.
			Span<char> token = new char[EntityMapData.MAPKEY_MAXLENGTH];
			mapData = MapEntity.ParseToken(mapData, token);

			// Check to see if we've finished or not.
			if (mapData.IsEmpty)
				break;

			if (token[0] != '{') {
				Error("MapEntity.ParseAllEntities: found %s when expecting {", token.ToString());
				continue;
			}

			// Parse the entity and add it to the spawn list.
			ReadOnlySpan<char> curMapData = mapData;
			mapData = ParseEntity(out BaseEntity entity, mapData, filter);
			if (entity == null)
				continue;

			if (entity.IsTemplate()) {
				// It's a template entity. Squirrel away its keyvalue text so that we can
				// recreate the entity later via a spawner. mapData points at the '}'
				// so we must add one to include it in the string.
				// Templates.Add(entity, curMapData, (mapData.Length - curMapData.Length) + 2); TODO

				// Remove the template entity so that it does not show up in FindEntityXXX searches.
				Util.Remove(entity);
				gEntList.CleanupDeleteList();
				continue;
			}

			// To
			if (entity is World) {
				// TODO entity.Parent = NULL_STRING; // don't allow a parent on the first entity (worldspawn)

				Util.DispatchSpawn(entity);
				continue;
			}

			// TODO

			// if (entity is NodeEnt ne) {
			// 	// We overflow the max edicts on large maps that have lots of entities.
			// 	// Nodes & Lights remove themselves immediately on Spawn(), so dispatch their
			// 	// spawn now, to free up the slot inside this loop.
			// 	// NOTE: This solution prevents nodes & lights from being used inside point_templates.
			// 	//
			// 	// NOTE: Nodes spawn other entities (ai_hint) if they need to have a persistent presence.
			// 	//		 To ensure keys are copied over into the new entity, we pass the mapdata into the
			// 	//		 node spawn function.
			// 	if (ne.Spawn(curMapData) < 0) {
			// 		gEntList.CleanupDeleteList();
			// 	}
			// 	continue;
			// }

			// if (entity is Light light) {
			// 	// We overflow the max edicts on large maps that have lots of entities.
			// 	// Nodes & Lights remove themselves immediately on Spawn(), so dispatch their
			// 	// spawn now, to free up the slot inside this loop.
			// 	// NOTE: This solution prevents nodes & lights from being used inside point_templates.
			// 	if (Util.DispatchSpawn(light) < 0) {
			// 		gEntList.CleanupDeleteList();
			// 	}
			// 	continue;
			// }

			// Build a list of all point_template's so we can spawn them before everything else
			if (entity is PointTemplate pt)
				pointTemplates.Add(pt);
			else {
				// Queue up this entity for spawning
				spawnList[numEntities].Entity = entity;
				spawnList[numEntities].Depth = 0;
				spawnList[numEntities].DeferredParentAttachment = null;
				spawnList[numEntities].DeferredParent = null;

				pSpawnMapData[numEntities].MapData = curMapData.ToString();
				pSpawnMapData[numEntities].MapDataLength = (mapData.Length - curMapData.Length) + 2;
				numEntities++;
			}
		}

#if false // TODO
		// Now loop through all our point_template entities and tell them to make templates of everything they're pointing to
		int templates = pointTemplates.Count;
		for (int i = 0; i < templates; i++) {
			PointTemplate pointTemplate = pointTemplates[i];

			// First, tell the Point template to Spawn
			if (Util.DispatchSpawn(pointTemplate) < 0) {
				Util.Remove(pointTemplate);
				gEntList.CleanupDeleteList();
				continue;
			}

			pointTemplate.StartBuildingTemplates();

			// Now go through all it's templates and turn the entities into templates
			int numTemplates = pointTemplate.GetNumTemplateEntities();
			for (int templateNm = 0; templateNm < numTemplates; templateNm++) {
				// Find it in the spawn list
				BaseEntity entity = pointTemplate.GetTemplateEntity(templateNm);
				for (int iEntNum = 0; iEntNum < numEntities; iEntNum++) {
					if (spawnList[iEntNum].Entity == entity) {
						// Give the point_template the mapdata
						pointTemplate.AddTemplate(entity, pSpawnMapData[iEntNum].MapData, pSpawnMapData[iEntNum].m_iMapDataLength);

						if (pointTemplate.ShouldRemoveTemplateEntities()) {
							// Remove the template entity so that it does not show up in FindEntityXXX searches.
							Util.Remove(entity);
							gEntList.CleanupDeleteList();

							// Remove the entity from the spawn list
							spawnList[iEntNum].Entity = null;
						}
						break;
					}
				}
			}

			pointTemplate.FinishBuildingTemplates();
		}
#endif

		SpawnHierarchicalList(numEntities, spawnList, activateEntities);
	}

	static int ComputeSpawnHierarchyDepth_r(BaseEntity? entity) {
		if (entity == null)
			return 1;

		// if (entity.Parent == NULL_STRING)
		// 	return 1;

		// BaseEntity parent = gEntList.FindEntityByName(null, ExtractParentName(entity.Parent));
		// if (parent == null)
		return 1;

		// if (parent == entity) {
		// 	Warning("LEVEL DESIGN ERROR: Entity %s is parented to itself!\n", entity.GetDebugName());
		// 	return 1;
		// }

		// return 1 + ComputeSpawnHierarchyDepth_r(parent);
	}

	static void ComputeSpawnHierarchyDepth(int entities, HierarchicalSpawn_t[] spawnList) {
		for (int nEntity = 0; nEntity < entities; nEntity++) {
			BaseEntity? entity = spawnList[nEntity].Entity;
			if (entity != null /* && !entity.IsDormant() */)//todo
				spawnList[nEntity].Depth = ComputeSpawnHierarchyDepth_r(entity);
			else
				spawnList[nEntity].Depth = 1;
		}
	}

	static void SpawnAllEntities(int numEntities, HierarchicalSpawn_t[] spawnList, bool activeEntities) {
		int nEntity;
		for (nEntity = 0; nEntity < numEntities; nEntity++) {
			BaseEntity? entity = spawnList[nEntity].Entity;

			// TODO
			// if (spawnList[nEntity].DeferredParent != null) {
			// 	BaseEntity pParent = spawnList[nEntity].DeferredParent;
			// 	int iAttachment = -1;
			// 	BaseAnimating pAnim = pParent.GetBaseAnimating();
			// 	if (pAnim != null) {
			// 		iAttachment = pAnim.LookupAttachment(spawnList[nEntity].DeferredParentAttachment);
			// 	}
			// 	entity.SetParent(pParent, iAttachment);
			// }

			if (entity != null) {
				if (Util.DispatchSpawn(entity) < 0) {
					for (int i = nEntity + 1; i < numEntities; i++) {
						// this is a child object that will be deleted now
						if (spawnList[i].Entity != null && spawnList[i].Entity!.IsMarkedForDeletion()) {
							spawnList[i].Entity = null;
						}
					}
					// Spawn failed.
					gEntList.CleanupDeleteList();
					// Remove the entity from the spawn list
					spawnList[nEntity].Entity = null;
				}
			}
		}

		if (activeEntities) {
			// bool asyncAnims = mdlcache.SetAsyncLoad(MDLCACHE_ANIMBLOCK, false);
			for (nEntity = 0; nEntity < numEntities; nEntity++) {
				BaseEntity? entity = spawnList[nEntity].Entity;
				// entity?.Activate(); todo
			}
			// mdlcache.SetAsyncLoad(MDLCACHE_ANIMBLOCK, asyncAnims);
		}
	}

	static void SpawnHierarchicalList(int entities, HierarchicalSpawn_t[] spawnList, bool activateEntities) {
		// Compute the hierarchical depth of all entities hierarchically attached
		ComputeSpawnHierarchyDepth(entities, spawnList);

		// Sort the entities (other than the world) by hierarchy depth, in order to spawn them in
		// that order. This insures that each entity's parent spawns before it does so that
		// it can properly set up anything that relies on hierarchy.
		// SortSpawnListByHierarchy(entities, spawnList); TODO

		// save off entity positions if in edit mode
		// if (engine.IsInEditMode()) // TODO
		// 	RememberInitialEntityPositions(entities, spawnList);

		// Set up entity movement hierarchy in reverse hierarchy depth order. This allows each entity
		// to use its parent's world spawn origin to calculate its local origin.
		// SetupParentsForSpawnList(entities, spawnList); TODO

		// Spawn all the entities in hierarchy depth order so that parents spawn before their children.
		SpawnAllEntities(entities, spawnList, activateEntities);
	}

	static ReadOnlySpan<char> ParseEntity(out BaseEntity? entity, ReadOnlySpan<char> EntData, IMapEntityFilter filter) {
		EntityMapData entData = new(EntData);
		Span<char> className = new char[EntityMapData.MAPKEY_MAXLENGTH];

		if (!entData.ExtractValue("classname", className))
			Error("classname missing from entity!\n");

		className = className.SliceNullTerminatedString();

		entity = null;
		if (filter == null || filter.ShouldCreateEntity(className)) {

			// Construct via the LINK_ENTITY_TO_CLASS factory.
			if (filter != null)
				entity = filter.CreateNextEntity(className);
			else
				entity = CreateEntityByName(className);

			// Set up keyvalues.
			if (entity != null) {
				// entity.ParseMapData(&entData);
			}
			else
				Warning($"Can't init {className}\n");

#if true // TODO: remove this once ParseMapData is implemented.
			Span<char> keyName = new char[EntityMapData.MAPKEY_MAXLENGTH];
			Span<char> value = new char[EntityMapData.MAPKEY_MAXLENGTH];
			if (entData.GetFirstKey(keyName, value))
				do { } while (entData.GetNextKey(keyName, value));
#endif
		}
		else {
			// Just skip past all the keys.
			Span<char> keyName = new char[EntityMapData.MAPKEY_MAXLENGTH];
			Span<char> value = new char[EntityMapData.MAPKEY_MAXLENGTH];
			if (entData.GetFirstKey(keyName, value)) {
				do {
				}
				while (entData.GetNextKey(keyName, value));
			}
		}

		// Return the current parser position in the data block
		return entData.CurrentBufferPosition();
	}
}

using SummonersAssociation.Items;
using SummonersAssociation.Models;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SummonersAssociation
{
	public class SummonersAssociation : Mod
	{
		internal static List<MinionModel> SupportedMinions;

		/// <summary>
		/// Mainly for "Counter" projectiles that count as minions but should not be teleported (by the Minion Control Rod)
		/// </summary>
		internal static Dictionary<int, Func<Projectile, bool>> TeleportConditionMinions;

		internal bool SupportedMinionsFinalized = false;

		public static SummonersAssociation Instance { get; private set; }
		
		internal static bool ProjectileFalse(Projectile p) => false;

		internal static HashSet<int> VanillaPersistentBuffsToRevert;

		/// <summary>
		/// Array of the different minion book types. Simple is 0, Normal is 1, Auto is 2
		/// </summary>
		public static int[] BookTypes;

		public override void Load() {
			Instance = this;

			LoadData();
		}

		private static void LoadData() {
			SupportedMinions = new List<MinionModel>() {
				new MinionModel(ItemID.BabyBirdStaff, BuffID.BabyBird, ProjectileID.BabyBird),
				new MinionModel(ItemID.AbigailsFlower, BuffID.AbigailMinion, ProjectileID.AbigailCounter),
				new MinionModel(ItemID.SlimeStaff, BuffID.BabySlime, ProjectileID.BabySlime),
				new MinionModel(ItemID.FlinxStaff, BuffID.FlinxMinion, ProjectileID.FlinxMinion),
				new MinionModel(ItemID.VampireFrogStaff, BuffID.VampireFrog, ProjectileID.VampireFrog),
				new MinionModel(ItemID.HornetStaff, BuffID.HornetMinion, ProjectileID.Hornet),
				new MinionModel(ItemID.ImpStaff, BuffID.ImpMinion, ProjectileID.FlyingImp),
				new MinionModel(ItemID.SpiderStaff, BuffID.SpiderMinion, new List<int>() { ProjectileID.VenomSpider, ProjectileID.JumperSpider, ProjectileID.DangerousSpider }),
				new MinionModel(ItemID.SanguineStaff, BuffID.BatOfLight, ProjectileID.BatOfLight),
				new MinionModel(ItemID.OpticStaff, BuffID.TwinEyesMinion, new List<int>() { ProjectileID.Retanimini, ProjectileID.Spazmamini }),
				new MinionModel(ItemID.PirateStaff, BuffID.PirateMinion, new List<int>() { ProjectileID.OneEyedPirate, ProjectileID.SoulscourgePirate, ProjectileID.PirateCaptain }),
				new MinionModel(ItemID.Smolstar, BuffID.Smolstar, ProjectileID.Smolstar),
				new MinionModel(ItemID.PygmyStaff, BuffID.Pygmies, new List<int>() { ProjectileID.Pygmy, ProjectileID.Pygmy2, ProjectileID.Pygmy3, ProjectileID.Pygmy4 }),
				new MinionModel(ItemID.StormTigerStaff, BuffID.StormTiger, ProjectileID.StormTigerGem),
				new MinionModel(ItemID.XenoStaff, BuffID.UFOMinion, ProjectileID.UFOMinion),
				new MinionModel(ItemID.RavenStaff, BuffID.Ravens, ProjectileID.Raven),
				new MinionModel(ItemID.TempestStaff, BuffID.SharknadoMinion, ProjectileID.Tempest),
				new MinionModel(ItemID.DeadlySphereStaff, BuffID.DeadlySphere, ProjectileID.DeadlySphere),
				// StardustDragonStaff: special treatment to count the 4 summoned projectiles (2x0.5f and 2x0f slots) by only concidering the second body part as 1 minion
				new MinionModel(ItemID.StardustDragonStaff, BuffID.StardustDragonMinion, new List<ProjModel>(){
					new ProjModel(ProjectileID.StardustDragon1, 0f),
					new ProjModel(ProjectileID.StardustDragon2, 1f),
					new ProjModel(ProjectileID.StardustDragon3, 0f),
					new ProjModel(ProjectileID.StardustDragon4, 0f),
				}),
				new MinionModel(ItemID.StardustCellStaff, BuffID.StardustMinion, ProjectileID.StardustCellMinion),
				new MinionModel(ItemID.EmpressBlade, BuffID.EmpressBlade, ProjectileID.EmpressBlade)
			};

			TeleportConditionMinions = new Dictionary<int, Func<Projectile, bool>>() {
				[ProjectileID.StormTigerGem] = ProjectileFalse,
				[ProjectileID.AbigailCounter] = ProjectileFalse
			};

			VanillaPersistentBuffsToRevert = new HashSet<int>();
		}

		public static void RegisterPersistentBuff(int buffID) {
			Main.persistentBuff[buffID] = true;
			Main.buffNoSave[buffID] = false;

			if (buffID < BuffID.Count) {
				VanillaPersistentBuffsToRevert.Add(buffID);
			}
		}

		internal static void RevertVanillaPersistentBuffs() {
			if (VanillaPersistentBuffsToRevert == null)
				return;

			//buffNoSave is only checked in Save IO, not Load, meaning that it will still restore the buff, but only once
			foreach (int buffID in VanillaPersistentBuffsToRevert) {
				Main.persistentBuff[buffID] = false;
				Main.buffNoSave[buffID] = true;
			}

			VanillaPersistentBuffsToRevert = null;
		}

		public override void PostSetupContent() {
			//don't change order here (simple is first, normal is second, automatic is third)
			BookTypes = new int[] {
				ModContent.ItemType<MinionLoadoutBookSimple>(),
				ModContent.ItemType<MinionLoadoutBook>(),
				ModContent.ItemType<MinionLoadoutBookAuto>()
			};

			if (ServerConfig.Instance.PersistentBuffs) {
				RegisterPersistentBuff(BuffID.Bewitched);
				RegisterPersistentBuff(BuffID.WarTable);
				RegisterPersistentBuff(BuffID.Summoning);
			}
		}

		public override void Unload() {
			SupportedMinions = null;
			TeleportConditionMinions = null;
			BookTypes = null;

			RevertVanillaPersistentBuffs();

			Instance = null;
		}

		public override object Call(params object[] args) {
			return SummonersAssociationSystem.Call(args);
		}

		public override void HandlePacket(BinaryReader reader, int whoAmI) {
			byte type = reader.ReadByte();

			if (Enum.IsDefined(typeof(PacketType), type)) {
				var packetType = (PacketType)type;
				switch (packetType) {
					case PacketType.SpawnTarget:
						MinionControlRod.HandleSpawnTarget(reader);
						break;
					case PacketType.ConfirmTargetToClient:
						MinionControlRod.HandleConfirmTargetToClient(reader);
						break;
					case PacketType.SyncPlayer:
						SummonersAssociationPlayer.ReceiveSyncPlayer(reader);
						break;
					default:
						Logger.Warn("'None' packet type received");
						break;
				}
			}
			else {
				Logger.Warn("Undefined packet type received: " + type);
			}
		}
	}
}

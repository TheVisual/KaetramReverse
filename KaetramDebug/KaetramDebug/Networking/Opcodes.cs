public static class Opcodes
{
	public enum Login
	{
		Login,
		Register,
	}

	public enum List
	{
		Spawns,
		Positions
	}

	public enum Equipment
	{
		Batch,
		Equip,
		Unequip,
		Style
	}

	public enum Movement
	{
		Request,
		Started,
		Step,
		Stop,
		Move,
		Follow,
		Entity,
		Speed
	}

	public enum Target
	{
		Talk,
		Attack,
		None,
		Object
	}

	public enum Combat
	{
		Initiate,
		Hit,
		Finish,
		Sync
	}

	public enum Projectile
	{
		Static,
		Dynamic,
		Create,
		Update,
		Impact
	}

	public enum Network
	{
		Ping,
		Pong,
		Sync
	}

	public enum Container
	{
		Batch,
		Add,
		Remove,
		Select,
		Swap
	}

	public enum Ability
	{
		Batch,
		Add,
		Update,
		Use,
		QuickSlot,
		Toggle
	}

	public enum Quest
	{
		Batch,
		Progress,
		Finish,
		Start
	}

	public enum Achievement
	{
		Batch,
		Progress
	}

	public enum Notification
	{
		Ok,
		YesNo,
		Text,
		Popup
	}

	public enum Experience
	{
		Sync,
		Skill
	}

	public enum NPC
	{
		Talk,
		Store,
		Bank,
		Enchant,
		Countdown
	}

	public enum Trade
	{
		Request,
		Add,
		Remove,
		Accept,
		Close,
		Open
	}

	public enum Enchant
	{
		Select,
		Confirm
	}

	public enum Guild
	{
		Create,
		Login,
		Logout,
		Join,
		Leave,
		Rank,
		Update,
		Experience,
		Banner,
		List,
		Error,
		Chat,
		Promote,
		Demote,
		Kick
	}

	public enum Pointer
	{
		Location, // Pointer on the map
		Entity,   // Pointer following an entity
		Relative,
		Remove
	}

	public enum Store
	{
		Open,
		Close,
		Buy,
		Sell,
		Update,
		Select
	}

	public enum Overlay
	{
		Set,
		Remove,
		Lamp,
		RemoveLamps,
		Darkness
	}

	public enum Camera
	{
		LockX,
		LockY,
		FreeFlow,
		Player
	}

	public enum Command
	{
		CtrlClick
	}

	public enum Skill
	{
		Batch,
		Update
	}

	public enum Minigame
	{
		TeamWar,
		Coursing
	}

	public enum MinigameState
	{
		Lobby,
		End,
		Exit
	}

	// Generic actions for when in a minigame.
	public enum MinigameActions
	{
		Score,
		End,
		Lobby,
		Exit
	}

	public enum Bubble
	{
		Entity,
		Position
	}

	public enum Effect
	{
		Add,
		Remove
	}

	public enum Friends
	{
		List,
		Add,
		Remove,
		Status,
		Sync
	}

	public enum Player
	{
		Login,
		Logout
	}

	public enum Crafting
	{
		Open,
		Select,
		Craft
	}

	public enum LootBag
	{
		Open,
		Take,
		Close
	}

	public enum Pet
	{
		Pickup
	}

	public enum Interface
	{
		Open,
		Close
	}
}

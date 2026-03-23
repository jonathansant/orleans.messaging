using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;

namespace Odin.Core.Auth.Permissions;

public interface IPermissionService
{
	HashSet<string> GetAllKeys();
	HashSet<string> GetAllExceptDisabledActions();
	ReadOnlyDictionary<string, string> GetAll();
	string? GetActionType(string permission);

	void Ensure(string permission);
	bool Contains(string permission);
	bool Contains(string permission, HashSet<string> permissions);

	HashSet<string> GetDisabledActions();
	HashSet<string> FilterDisabledActions(HashSet<string> permissions);

	/// <summary>
	/// Validate permissions.
	/// </summary>
	/// <param name="permission">permissions</param>
	/// <returns>Invalid permissions.</returns>
	HashSet<string>? Validate(List<string>? permission);

	/// <summary>
	/// Check permission if it's allowed and not disabled.
	/// </summary>
	/// <param name="permission">Permission to check.</param>
	/// <param name="permissions">Permissions to evaluate.</param>
	/// <returns>Returns true if allowed and not disabled.</returns>
	bool IsEligible(string permission, HashSet<string> permissions);
}

public abstract class PermissionService : IPermissionService
{
	protected abstract HashSet<string> DisabledActions { get; }
	protected abstract string AnyKeyword { get; }

	private string AnyPermission { get; }
	private readonly Dictionary<string, string> _items = new();
	private ReadOnlyDictionary<string, string> _itemsReadOnly;
	private HashSet<string> _itemsExceptDisabled;
	private HashSet<string> _allPermissions;

	protected PermissionService(
		IServiceProvider serviceProvider
	)
	{
		var config = serviceProvider.GetRequiredService<IConfiguration>();
		var permissionLocator = serviceProvider.GetRequiredService<IPermissionLocator>();

		AnyPermission = $".{AnyKeyword}";
		Initialize(config, permissionLocator);
	}

	public HashSet<string> GetAllKeys() => _allPermissions;

	public ReadOnlyDictionary<string, string> GetAll() => _itemsReadOnly;

	public void Ensure(string permission)
	{
		if (Contains(permission)) return;

		throw new InvalidOperationException($"'{permission}' permission not supported.");
	}

	public bool Contains(string permission)
		=> Contains(permission, _allPermissions);

	public bool Contains(string permission, HashSet<string> permissions)
	{
		var isAny = permission.EndsWith(AnyPermission);

		if (!isAny)
			return permissions.Contains(permission);

		permission = permission.Substring(0, permission.Length - AnyPermission.Length);
		return permissions.FirstOrDefault(p => p.StartsWith(permission)) != null;
	}

	public HashSet<string> FilterDisabledActions(HashSet<string> permissions)
	{
		var userDisabledActions = DisabledActions.Intersect(permissions);
		foreach (var userDisabledActionPermission in userDisabledActions)
		{
			var actionType = GetActionType(userDisabledActionPermission);
			permissions = permissions.Where(x => !x.EndsWith(actionType)).ToHashSet();
		}

		return permissions;
	}

	public HashSet<string>? Validate(List<string>? permissions) => permissions?.Except(GetAllKeys()).ToHashSet();

	protected abstract string MapDisabledActionTypes(string permissionActionType, string permission);

	public virtual bool IsEligible(string permission, HashSet<string> permissions)
	{
		if (permission.EndsWith(AnyPermission))
			return true;

		var permissionActionType = GetActionType(permission);
		var disabledActionPermission = MapDisabledActionTypes(permissionActionType, permission);
		var hasDisabledAction = permissions.Contains(disabledActionPermission);
		return !hasDisabledAction;
	}

	public string? GetActionType(string permission)
	{
		Ensure(permission);
		return _itemsReadOnly.GetValueOrDefault(permission);
	}

	public HashSet<string> GetDisabledActions() => DisabledActions;

	public HashSet<string> GetAllExceptDisabledActions()
		=> _itemsExceptDisabled;

	private void Initialize(IConfiguration config, IPermissionLocator permissionLocator)
	{
		var locatorPermissions = permissionLocator.GetAll();

		foreach (var (permission, value) in locatorPermissions)
			_items.Add(permission, value.ActionType);

		var configPermissions = config.GetSection("permissions").Get<HashSet<string>>();
		configPermissions ??= new();
		foreach (var permission in configPermissions)
			_items.Add(permission, permission.SubstringLastIndexOf('.'));

		_itemsReadOnly = new(_items);
		_allPermissions = _itemsReadOnly.Keys.ToHashSet();
		_itemsExceptDisabled = _itemsReadOnly.Keys.Except(DisabledActions).ToHashSet();
	}
}

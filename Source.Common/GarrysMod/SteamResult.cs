using Steamworks;

namespace Source.Common.GarrysMod;

public static class SteamResult
{
	public static string ToString(EResult result) {
		return result switch {
			EResult.k_EResultOK => "OK",
			EResult.k_EResultFail => "Generic failure",
			EResult.k_EResultNoConnection => "No internet connection",
			EResult.k_EResultLoggedInElsewhere => "Logged in elsewhere",
			EResult.k_EResultInvalidParam => "Invalid parameter (Weird symbols in name/descrption?)",
			EResult.k_EResultFileNotFound => "File not found",
			EResult.k_EResultBusy => "Method is busy",
			EResult.k_EResultAccessDenied => "Access denied (Item hidden/banned?)",
			EResult.k_EResultTimeout => "Timed out",
			EResult.k_EResultBanned => "Item/user is banned",
			EResult.k_EResultAccountNotFound => "Account not found",
			EResult.k_EResultServiceUnavailable => "Service unavailable",
			EResult.k_EResultNotLoggedOn => "Not logged on",
			EResult.k_EResultInsufficientPrivilege => "Insufficient privilege",
			EResult.k_EResultLimitExceeded => "Limit exceeded",
			EResult.k_EResultLogonSessionReplaced => "Log on session replaced (Your GSLT is used elsewhere)",
			EResult.k_EResultIOFailure => "Input/output failure",
			EResult.k_EResultSuspended => "Operation suspended",
			EResult.k_EResultCancelled => "Operation cancelled",
			EResult.k_EResultDiskFull => "Disk drive full",
			EResult.k_EResultItemDeleted => "Item(GSLT?) deleted",
			EResult.k_EResultTimeNotSynced => "Time is not synched",
			EResult.k_EResultGSLTDenied => "GSL token banned",
			EResult.k_EResultGSOwnerDenied => "GS owner denied",
			EResult.k_EResultGSLTExpired => "GSL token expired",
			EResult.k_EResultLimitedUserAccount => "Limited user account",
			_ => "Steam error code " + (int)result,
		};
	}}

using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using UnityEditor;
using UnityEngine;

// Verifies the UGS side of multiplayer without playing the game:
//   Unity.exe -batchmode -projectPath <proj> -executeMethod UGSCheck.Verify -logFile <log>
// It initializes Unity Services, signs in anonymously (no identity provider
// needed), then creates a Relay allocation and prints a join code. Exit 0 = all
// good; exit 1 = prints the failure reason.
public static class UGSCheck
{
    public static void Verify()
    {
        RunAsync();
    }

    static async void RunAsync()
    {
        try
        {
            Debug.Log("UGS: cloudProjectId = " +
                      (string.IsNullOrEmpty(Application.cloudProjectId) ? "(none - project not linked)" : Application.cloudProjectId));

            await UnityServices.InitializeAsync();
            Debug.Log("UGS: services initialised");

            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log("UGS: signed in anonymously as " + AuthenticationService.Instance.PlayerId);

            var allocation = await RelayService.Instance.CreateAllocationAsync(1);
            string code = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            Debug.Log("UGS: Relay OK - join code " + code);

            EditorApplication.Exit(0);
        }
        catch (System.Exception e)
        {
            Debug.LogError("UGS: FAILED - " + e.GetType().Name + ": " + e.Message);
            EditorApplication.Exit(1);
        }
    }
}

/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Unity.Analyzers.Tests;

public class UnityObjectImplicitBoolTests : BaseCodeFixVerifierTest<UnityObjectImplicitBoolAnalyzer, UnityObjectImplicitBoolCodeFix>
{

	[Fact]
	public async Task IsImplicitBoolOperatorOnUnityObject()
	{
		const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    public Transform a = null;

    public void Update()
    {
        if (a) { }
    }
}
";

		var diagnostic = ExpectDiagnostic(UnityObjectImplicitBoolAnalyzer.Rule)
			.WithLocation(10, 13);

		await VerifyCSharpDiagnosticAsync(test, diagnostic);
	}

	[Fact]
	public async Task IsInvertedImplicitBoolOperatorOnUnityObject()
	{
		const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    public Transform a = null;

    public void Update()
    {
        if (!a) { }
    }
}
";

		var diagnostic = ExpectDiagnostic(UnityObjectImplicitBoolAnalyzer.Rule)
			.WithLocation(10, 13);

		await VerifyCSharpDiagnosticAsync(test, diagnostic);
	}

	[Fact]
	public async Task FixImplicitBoolOperatorOnUnityObject()
	{
		const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    public Transform a = null;

    public void Update()
    {
        if (a) { }
    }
}
";

		var diagnostic = ExpectDiagnostic(UnityObjectImplicitBoolAnalyzer.Rule)
			.WithLocation(10, 13);

		await VerifyCSharpDiagnosticAsync(test, diagnostic);

		const string fixedTest = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    public Transform a = null;

    public void Update()
    {
        if (a != null) { }
    }
}
";
		await VerifyCSharpFixAsync(test, fixedTest);
	}

	[Fact]
	public async Task FixInvertedImplicitBoolOperatorOnUnityObject()
	{
		const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    public Transform a = null;

    public void Update()
    {
        if (!a) { }
    }
}
";

		var diagnostic = ExpectDiagnostic(UnityObjectImplicitBoolAnalyzer.Rule)
			.WithLocation(10, 13);

		await VerifyCSharpDiagnosticAsync(test, diagnostic);

		const string fixedTest = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    public Transform a = null;

    public void Update()
    {
        if (a == null) { }
    }
}
";
		await VerifyCSharpFixAsync(test, fixedTest);
	}
}

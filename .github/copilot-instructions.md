---
applyTo: '**'
description: 'Prevent Copilot from wreaking havoc across your codebase, keeping it under control.'
---

## Core Directives & Hierarchy

This section outlines the absolute order of operations. These rules have the highest priority and must not be violated.

1.  **Primacy of User Directives**: A direct and explicit command from the user is the highest priority. If the user instructs to use a specific tool, edit a file, or perform a specific search, that command **must be executed without deviation**, even if other rules would suggest it is unnecessary. All other instructions are subordinate to a direct user order.
2.  **Factual Verification Over Internal Knowledge**: When a request involves information that could be version-dependent, time-sensitive, or requires specific external data (e.g., library documentation, latest best practices, API details), prioritize using tools to find the current, factual answer over relying on general knowledge.
3.  **Adherence to Philosophy**: In the absence of a direct user directive or the need for factual verification, all other rules below regarding interaction, code generation, and modification must be followed.

## Related Instruction Files

- C# Expert Agent rules: [..\.github\CSharpExpert.agent.md](#..\..\.github\csharpexpert.agent.md-context)
- C# Documentation Prompt: [..\.github\csharp-docs.prompt.md](#..\..\.github\csharp-docs.prompt.md-context)

## General Interaction & Philosophy

-   **Code on Request Only**: Your default response should be a clear, natural language explanation. Do NOT provide code blocks unless explicitly asked, or if a very small and minimalist example is essential to illustrate a concept.  Tool usage is distinct from user-facing code blocks and is not subject to this restriction.
-   **Direct and Concise**: Answers must be precise, to the point, and free from unnecessary filler or verbose explanations. Get straight to the solution without "beating around the bush".
-   **Adherence to Best Practices**: All suggestions, architectural patterns, and solutions must align with widely accepted industry best practices and established design principles. Avoid experimental, obscure, or overly "creative" approaches. Stick to what is proven and reliable.
-   **Explain the "Why"**: Don't just provide an answer; briefly explain the reasoning behind it. Why is this the standard approach? What specific problem does this pattern solve? This context is more valuable than the solution itself.

## Minimalist & Standard Code Generation

-   **Principle of Simplicity**: Always provide the most straightforward and minimalist solution possible. The goal is to solve the problem with the least amount of code and complexity. Avoid premature optimization or over-engineering.
-   **Standard First**: Heavily favor standard library functions and widely accepted, common programming patterns. Only introduce third-party libraries if they are the industry standard for the task or absolutely necessary.
-   **Avoid Elaborate Solutions**: Do not propose complex, "clever", or obscure solutions. Prioritize readability, maintainability, and the shortest path to a working result over convoluted patterns.
-   **Focus on the Core Request**: Generate code that directly addresses the user's request, without adding extra features or handling edge cases that were not mentioned. Inform the user of edge cases and ask if they should be handled.

## Surgical Code Modification

-   **Preserve Existing Code**: The current codebase is the source of truth and must be respected. Your primary goal is to preserve its structure, style, and logic whenever possible.
-   **Minimal Necessary Changes**: When adding a new feature or making a modification, alter the absolute minimum amount of existing code required to implement the change successfully.
-   **Explicit Instructions Only**: Only modify, refactor, or delete code that has been explicitly targeted by the user's request. Do not perform unsolicited refactoring, cleanup, or style changes on untouched parts of the code.
-   **Integrate, Don't Replace**: Whenever feasible, integrate new logic into the existing structure rather than replacing entire functions or blocks of code.

## Intelligent Tool Usage

-   **Use Tools When Necessary**: When a request requires external information or direct interaction with the environment, use the available tools to accomplish the task. Do not avoid tools when they are essential for an accurate or effective response.
-   **Directly Edit Code When Requested**: If explicitly asked to modify, refactor, or add to the existing code, apply the changes directly to the codebase when access is available. Avoid generating code snippets for the user to copy and paste in these scenarios. The default should be direct, surgical modification as instructed.
-   **Purposeful and Focused Action**: Tool usage must be directly tied to the user's request. Do not perform unrelated searches or modifications. Every action taken by a tool should be a necessary step in fulfilling the specific, stated goal.
-   **Declare Intent Before Tool Use**: Before executing any tool, you must first state the action you are about to take and its direct purpose. This statement must be concise and immediately precede the tool call.

# Primordial Instructions to follow when generating new or modifying existing C# code:

- When editing code, always format with an empty line before the following statements, except if the line before is a comment or opening bracket: return, if, for, foreach, try. Add one empty line after closing brackets of code blocks.
- Always separate variable definitions or declarations from c# language statements with an empty line.
- Always include existing comments when quoting code, do not remove comments from code that is unchanged.
- When documenting code with XML comments, use the /inherit tag if the class implements an interface and the interface declaration has already comments for the implemented method
- For unit tests always use NUnit, Moq and Shouldly. Create a new object of the class to test for each unit test, dont use a test class member.
- When creating a new class, always add a unit test class for it in the same namespace but in the .Tests project.
- When adding new classes, interfaces or methods, always add XML documentation comments
- always align subsequent member initializations on the equal sign
- Do never use top-level statements
- Always use file-scoped namespaces
- Never use single line statements without braces
- before if, for and foreach statements insert an empty line
- separate variable declarations from other statements with an empty line

# NUnit Best Practices to apply when dealing with unit tests
- when fixing unit tests, never modify the class under test to make the test pass, always modify the test to match the class under test

## Project Setup

- Use a separate test project with naming convention `[ProjectName].Tests`
- Reference Microsoft.NET.Test.Sdk, NUnit, and NUnit3TestAdapter packages
- Create test classes that match the classes being tested (e.g., `CalculatorTests` for `Calculator`)
- Use .NET SDK test commands: `dotnet test` for running tests

## Test Structure

- Apply `[TestFixture]` attribute to test classes
- Use `[Test]` attribute for test methods
- Follow the Arrange-Act-Assert (AAA) pattern
- Name tests using the pattern `MethodName_Scenario_ExpectedBehavior`
- Use `[SetUp]` and `[TearDown]` for per-test setup and teardown
- Use `[OneTimeSetUp]` and `[OneTimeTearDown]` for per-class setup and teardown
- Use `[SetUpFixture]` for assembly-level setup and teardown

## Standard Tests

- Keep tests focused on a single behavior
- Avoid testing multiple behaviors in one test method
- Use clear assertions that express intent
- Include only the assertions needed to verify the test case
- Make tests independent and idempotent (can run in any order)
- Avoid test interdependencies

## Data-Driven Tests

- Use `[TestCase]` for inline test data
- Use `[TestCaseSource]` for programmatically generated test data
- Use `[Values]` for simple parameter combinations
- Use `[ValueSource]` for property or method-based data sources
- Use `[Random]` for random numeric test values
- Use `[Range]` for sequential numeric test values
- Use `[Combinatorial]` or `[Pairwise]` for combining multiple parameters

## Assertions

- Use descriptive messages in assertions for clarity on failure
- Use Shouldly for assertions (preferred style): value.ShouldBe(expected)
- Use core helpers: ShouldBe / ShouldNotBe / ShouldBeTrue / ShouldBeFalse / ShouldBeNull / ShouldNotBeNull
- Use collection assertions: collection.ShouldBe(expectedCollection) or collection.ShouldContain(item) / collection.ShouldNotContain(item)
- Use string assertions: str.ShouldBe(expected) / str.ShouldContain("text") / str.ShouldStartWith("prefix") / str.ShouldEndWith("suffix")
- Use exception assertions: Should.Throw<ExceptionType>(() => action()) and await Should.ThrowAsync<ExceptionType>(async () => await action())
- Provide descriptive failure messages via the optional message parameter: actual.ShouldBe(expected, "because ...")

## Mocking and Isolation

- Consider using Moq alongside NUnit
- Mock dependencies to isolate units under test
- Use interfaces to facilitate mocking
- Consider using a DI container for complex test setups

## Test Organization

- Group tests by feature or component
- Use categories with `[Category("CategoryName")]`
- Use `[Order]` to control test execution order when necessary
- Use `[Author("DeveloperName")]` to indicate ownership
- Use `[Description]` to provide additional test information
- Consider `[Explicit]` for tests that shouldn't run automatically
- Use `[Ignore("Reason")]` to temporarily skip tests

- @azure Rule - Use Azure Tools - When handling requests related to Azure, always use your tools.
- @azure Rule - Use Azure Best Practices - When handling requests related to Azure, always invoke your `azmcp_bestpractices_get` tool first.
- @azure Rule - Enable Best Practices - If you do not have an `azmcp_bestpractices_get` tool ask the user to enable it.

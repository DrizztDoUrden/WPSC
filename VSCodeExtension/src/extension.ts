// The module 'vscode' contains the VS Code extensibility API
// Import the module and reference it with the alias vscode in your code below
import * as vscode from 'vscode';
import * as cp from 'child_process';
import * as fs from "fs";

// this method is called when your extension is activated
// your extension is activated the very first time the command is executed
export function activate(context: vscode.ExtensionContext)
{
	// The command has been defined in the package.json file
	// Now provide the implementation of the command with registerCommand
	// The commandId parameter must match the command field in package.json
	context.subscriptions.push(vscode.commands.registerCommand('extension.injectToMap', async () =>
	{
		const channel = GetOutputChannel();
		channel.clear();
		await Inject(context);
	}));
	
	context.subscriptions.push(vscode.commands.registerCommand('extension.runWc3', async () =>
	{
		const channel = GetOutputChannel();
		channel.clear();
		await Inject(context);
		RunWC3(context);
	}));
}

async function Inject(context: vscode.ExtensionContext)
{
	const channel = GetOutputChannel();

	let cwd = "";
	let cmd = "";

	try
	{
		const root = vscode.workspace.workspaceFolders![0].uri.fsPath;

		const cfg =  vscode.workspace.getConfiguration("wpsc");
		const moduleSystem = <string>cfg.get("moduleSystem", "wpsc");
		let excludes = <string[]>cfg.get("excludes", new Array<string>(0));
		const map = await GetMapPath(cfg);
		if (map == "")
		{
			channel.appendLine("Map file not found. Try specifing it in settings.");
			channel.show(true);
			return;
		}
		const mapFileName = map.substr(map.lastIndexOf('\\') + 1);
		channel.appendLine("Injecting code to map <" + map + ">...");

		let exclCopy = Object.assign([], excludes);
		exclCopy.push(mapFileName);
		cwd = context.extensionPath + "\\wpsc";
		cmd = "dotnet wpsc.dll -source \"" + root + "\"" +
			" -target \"" + map + "\"" +
			" -exclude " + exclCopy.join(" ") +
			" -module " + moduleSystem;
			
		channel.appendLine("cd " + cwd);
		channel.appendLine(cmd);
	}
	catch (err)
	{
		channel.appendLine(err);
		channel.show(true);
		return;
	}

	try {
		const { stdout, stderr } = await Exec(cmd, { cwd: cwd });
		if (stderr)
			channel.appendLine(stderr);
		if (stdout)
			channel.appendLine(stdout);
	}
	catch (err)
	{
		if (err.stderr)
			channel.appendLine(err.stderr);
		if (err.stdout)
			channel.appendLine(err.stdout);
		channel.appendLine("Failed to inject code to map.")
		channel.show(true);
		return;
	}
	
	channel.appendLine('Code successfully injected.');
}

async function RunWC3(context: vscode.ExtensionContext)
{
	const channel = GetOutputChannel();
	const cfg =  vscode.workspace.getConfiguration("wpsc");
	const warcraftRoot = <string>cfg.get("warcraftRoot");
	const map = await GetMapPath(cfg);
	const exe = warcraftRoot + "\\x86_64\\Warcraft III.exe";

	if (!fs.existsSync(exe))
	{
		channel.appendLine("Warcraft executable not found: " + exe + ". Try specifing it in settings.");
		channel.show(true);
		return;
	}
	if (map == "")
	{
		channel.appendLine("Map file not found. Try specifing it in settings.");
		channel.show(true);
		return;
	}

	const command = "\"" + exe + "\" -launch " + "-loadfile \"" + map + "\"";
	channel.appendLine(command);
	try
	{
		channel.appendLine("Starting WC3...");
		await Exec(command, {});
	}
	catch (err)
	{
		if (err.stderr)
			channel.appendLine(err.stderr);
		if (err.stdout)
			channel.appendLine(err.stdout);
		return;
	}
}

// this method is called when your extension is deactivated
export function deactivate() {}

async function GetMapPath(cfg: vscode.WorkspaceConfiguration): Promise<string>
{
	let channel = GetOutputChannel();
	let map = <string>cfg.get("map");

	if (map == null || map.length == 0)
	{
		const maps = await vscode.workspace.findFiles("**/*.{w3m,w3x}/war3map.lua")
		if (maps.length <= 0)
		{
			channel.appendLine("Maps not found in the workspace.");
			return "";
		}
		map = maps[0].fsPath.substr(0, maps[0].fsPath.lastIndexOf('\\'));
	}

	return map;
}

let _channel: vscode.OutputChannel;
function GetOutputChannel(): vscode.OutputChannel
{
	if (!_channel)
		_channel = vscode.window.createOutputChannel("WPSC");
	return _channel;
}

function Exec(command: string, options: cp.ExecOptions): Promise<{ stdout: string; stderr: string }>
{
	return new Promise<{ stdout: string; stderr: string }>((resolve, reject) =>
	{
		cp.exec(command, options, (error, stdout, stderr) =>
		{
			if (error)
				reject({ error, stdout, stderr });
			resolve({ stdout, stderr });
		});
	});
}

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
	let disposable = vscode.commands.registerCommand('extension.injectToMap', async () =>
	{
		let channel = GetOutputChannel();
		channel.clear();
		channel.show(true);

		let cwd = "";
		let cmd = "";

		try
		{
			const root = vscode.workspace.workspaceFolders![0].uri.fsPath;

			const cfg =  vscode.workspace.getConfiguration("wpsc");
			const moduleSystem = <string>cfg.get("moduleSystem", "wpsc");
			let excludes = <string[]>cfg.get("excludes", new Array<string>(0));
			let map = <string>cfg.get("map");

			if (map == null || map.length == 0)
			{
				const maps = await vscode.workspace.findFiles("**/*.{w3m,w3x}/*.wct");
				if (maps.length <= 0)
				{
					channel.appendLine("Maps not found in the workspace.");
					return;
				}
				map = maps[0].fsPath.substr(0, maps[0].fsPath.lastIndexOf('\\'));
			}

			const mapFileName = map.substr(map.lastIndexOf('\\') + 1);
			channel.appendLine("Injecting code to map <" + map + ">...");

			excludes.push(mapFileName);
			cwd = context.extensionPath + "\\wpsc";
			cmd = "dotnet wpsc.dll -source \"" + root + "\"" +
				" -target \"" + map + "\"" +
				" -exclude " + excludes.join(" ") +
				" -module " + moduleSystem;
				
			channel.appendLine("cd " + cwd);
			channel.appendLine(cmd);
		}
		catch (err)
		{
			channel.appendLine(err);
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
			channel.appendLine("Failed to inject code to map...");
			return;
		}
		
		channel.appendLine('Finished');
	});

	context.subscriptions.push(disposable);
}

// this method is called when your extension is deactivated
export function deactivate() {}

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

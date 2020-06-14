# BLMain
**Rename your `master` branch to `main` across your GitHub repositories**

As a society and as an industry, we're currently being asked an important question:
**What will we do to stand against systemic injustice?**
The answers to that question will rarely be easy or simple and many of them will require us to
step away from the technology we build our careers upon and instead work with people. Most of
these challenges cannot be solved by writing code, but in some small cases we can still help.

This tool is designed to make the process of migrating your GitHub repositories to a `main` branch
simple, straightforward and (relatively) painless. My intent is that, by providing this tool,
we can all spend a bit more time working on the hard problems and less on the small ones.

## Usage
By default the application runs in a read-only mode, printing out the steps it would take on each repository but not
actually making these changes. When you are happy with the changes that will be applied, you can supply the `--apply true`
command line parameter to make them.

**NOTE** You'll need a [Personal Access Token](https://github.com/settings/tokens/new) if you wish to make
changes to your repositories. When generating one, please ensure that you grant access to the `repo` scope.

The easiest way to run this tool is using `docker run --rm -it sierrasoftworks/blmain $OPTS`, however
you can also build it using `dotnet publish -o ./out` if you've got the .NET Core 3.1 SDK installed.

#### For a User
If you're running this against your own GitHub account, you can specify your account
name with `--user $YOUR_USERNAME`.

```bash
blmain --token $YOUR_ACCESS_TOKEN --user $YOUR_USERNAME --apply true
```

#### For an Organization
```bash
blmain --token $YOUR_ACCESS_TOKEN --org $YOUR_ORG --apply true
```

#### For a Specific set of Repositories
You can limit the repositories that this tool runs against by providing a regex `--filter`
parameter. This will apply against the repository name.

```bash
blmain --token $YOUR_ACCESS_TOKEN --org $YOUR_ORG --apply true --filter "^prefix-"
```

#### Include Forks
By default this tool will not make changes to forks of repositories, with the expectation that
the upstream repository will need to make these changes. If you'd prefer to make these changes
anyway, you can do so by passing `--update-forks true`.

```bash
blmain --token $YOUR_ACCESS_TOKEN --user $YOUR_USERNAME --apply true --update-forks true
```
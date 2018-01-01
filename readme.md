# Empyrion Mod Hotloader

## FAQ

### Q: What is this?

One of the probelms that server owners reported a lot was frustration with needing server access to load, unload / flush mods.  So, I created a mod that allows you to manage mods.

### Q: How do I use it?

It's prettye asy, but it will take a couple of steps to set up.

The first thing you're going to want to do is copy the contents of the `Release` folder in this repository into your dedicated server's `Content/Mods` folder, and then restart your server.  Great, now it's working, but it's not doing anything.

Next, you're going to have to make sure that your server's admin config file is set up.  Make sure that your steam id is listed in the `Elevated` section of the `adminconfig.yaml` found in the `Saves` folder of your server.  Note that in order to interact with the mod from in-game, you must be listed in the `adminconfig.yaml` file

In order to allow the mod to manage your other mods, you have to move the `.dll` files for those mods (and only the `.dll` files) into the `watched` folder in in the `Hotloader` directory that you just copied.  The hotloader will monitor the `watched` folder for changes.  Not that the hotloader only monitors the watched folder, it won't interact with mods that are in other folders.

Once you've copied your mod into the `watched` folder, log into your server using the Empyrion client.  to list all of the mods that are being monitored, bring up a chat bar, and type, `\mod list`

That will list the mods that are currently being watched.  Note that the first column is ID of the mod, to activate it type, `\mod activate {ID}` where `{ID}` is the id of the mod you want to activate.  to deactivate a mod type, `\mod deactivate {ID}` and to completely flush the mod, type `\mod flush {ID}`.

### Q: Can your UI be.... better?

I would love to make a better UI for you, but this is kind of just the current state of UIs in Empyrion's mod API as I understand it.  If you've got a better idea, let me know, or better yet, submit a pull request!

### Q: Does it work with my mod?

Probably, but I don't know.  I've tested it with a bunch of mods that I've written, but I haven't teseted with everything.  In theory it should work, and if it doesn't, let me know and I'll see if I can't help figure out what's going wrong.

### Q: I dug into your code, what the hell is "Broker"?

That's the development version of a library that I'm working to make building mods easier.  I haven't released it, but I'll update this repo when I have.  For now, if you want to use it, it can be found in the `dependencies` folder of this repo.

### Q: Do you accept pull requests?

My fondest dreams are of pull requests.

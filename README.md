# twwstats-dataexporter
Total War: WARHAMMER data exporter for twwstats. This projects heavily leverages code from the [packfilemanager](https://sourceforge.net/projects/packfilemanager/) project. The goal of this project is twofold:
- extract raw tables data from `.pack` files and/or Assembly Kit `.xml` files and convert each table to a JSON file (array of objects)
- extract images from `.pack` files and store them in a folder structure matching the one found inside the `.pack` files

# Keeping the master_schema.xml up to date
Updated versions of the master_schema.xml file are regularly uploaded to [packfilemanager](https://sourceforge.net/projects/packfilemanager/) when new versions of the game are released. Note however that it contains a few errors that are problematic to twwstats and must be manually corrected (I've tried getting in contact with PFM's owner/contributors to report it but never got a hold of them unfortunately...)

## Local changes
- battle_entities_tables
    - hitpoints -> hit_points
- missile_weapons_tables
    - can_fire_at_buildings -> use_secondary_ammo_pool
- special_ability_phases_tables
    - max_damaged_entites -> max_damaged_entities


# Configurations
All the configurations you can make are inside the `Common.cs` file.

## Game Versions
This is the big list of versions stored inside the `GameVersions` property. You should only list the versions you have the data for in the Data Folder (see below). Note that I am using the Steam ManifestId as the folder name but this is just my convention; you can use different folder names if you prefer.

## Data Folder
The Data Folder (`DataPath` property of `Common.cs`) is the location on your computer where you store the raw .pack and .xml files for each game. It should contain the following subfolders
- tww -> Put the data folders for TWW game versions here
- tww2 -> Put the data folders for TWW2 game versions here
- mods -> Put the data folders for mods versions here
- Export -> This is where the json files get exported for each version when running the exporter

### Game Version Folder(s)
In the appropriate Data Folder's subfolder (e.g. `<DataFolder>/tww2` for a game version defined with `Game.TWW2` in the `GameVersions` List), create a folder matching the name you gave to your version (i.e. the third element of the Tuple inside `GameVersions`) and copy all `data*.pack` and `local*.pack` files from `<steamapps>\common\Total War WARHAMMER II\data` into that folder (these are the files for the current version of the game installed on your computer. For older versions seee below).

Optionally you can also copy all the `.xml` files found under `<steamapps>\common\Total War WARHAMMER II\assembly_kit\raw_data\db` (those are the Assembly Kit files). If they exist they will be used instead of the `.pack` files to get the tables data (the `.pack` files are still required for the images).

# Eventual changes
This project will eventually be deprecated in favor of a similar project based on the [Rusted PackFile Manager (rpfm)](https://github.com/Frodo45127/rpfm) once I wrap my head around Rust :)

# Getting older versions of the game
This is just some information for those of you who would like to get a hold of older versions of the game.

*Guide:* https://www.reddit.com/r/Steam/comments/611h5e/guide_how_to_download_older_versions_of_a_game_on/  
download_depot \<appid> \<depotid> [\<target manifestid>] [\<delta manifestid>] [\<depot flags filter>] : download a single depot 

*TWW*  
**APP ID**: 364360  
**Data Depot Id**: 364362  
**English Depot Id**: 364363  
**TWW Data Versions:** https://steamdb.info/depot/364362/manifests/  
**TWW English Versions:** https://steamdb.info/depot/364363/manifests/  
*Cmd Example:* download_depot 364360 364362 8016606766435871385 -> Versy first data version!  

*TWW2*  
**APP ID**: 594570  
**Data Depot Id**: 594572  
**English Depot Id**: 594573  
**TWW Data Versions:** https://steamdb.info/depot/594572/manifests/  
**TWW English Versions:** https://steamdb.info/depot/594573/manifests/  
*Cmd Example:* download_depot 594570 594572 5566353893085820817 -> Make War Not Love patch (latest at this time)

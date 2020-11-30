Dynamic Audio in Unity
Par Alexy Duhamel
------------------------------------------

##########################################
1 ) Importation dans un projet
##########################################

1 ) Déplacer les dossiers "Audio" et "Scripts" dans le dossier "Asset" de votre projet Unity
2 ) Créer un GameObjet dans unity (clique droit => create empty)
3 ) Lier le script "AudioManager" au GameObject fraichement créé (glisser le script dans le GameObject)
4 ) Pour ajouter des sons à l'audio manager, renseigner le champs "size" avec le nombre
    de fichiers audios que vous désirez mettre puis glisser-déposer depuis le dossier "Audio" dans le champ "clip" de chaque Element

##########################################
2 ) Fonctionnement
##########################################

On regroupe les fichiers audio dans un tableau géré par le script AudioManager.
Ainsi on peut ajouter ou enlever des sons facilement directement depuis l'interface
Le mixer master est lié au script AudioManager, on peut donc modifier directement dans le mixer les effets que l'ont veut appliquer.

# Cloudflare-Bypass (Under Attack mode)

Librairies C# et python pour envoyer une requête HTTP sur un site bloqué par la page d'attente de Cloudflare.

## Dépendance

Aucune dépendance nécessaire

## Problématique

Il y a peu j'ai eu besoin de faire des requêtes HTTP sur un site pour récupérer certaines informations dessus. Cependant le site en question était protégé par Cloudflare. En temps normal Cloudflare ne gêne pas les requêtes HTTP et vous renvoie directement le page demander. Cependant quand un site se fait DDOS, l'administrateur peut demander à Cloudflare de passer son site un mode "Under Attack" ce qui permet de filtrer les requêtes abusives.

Si vous accédez à un site en mode "Under Attack" pour la première fois dans la journée vous aurez d'abord accès à une page Cloudflare qui va vous demander d'attendre quelques secondes pour vous rediriger sur le page demandé.

Le problème se situe sur cette page d'attente. Si vous faites une requête HTTP sur le site vous obtiendrez la page d'attente, cependant pour être redirigé il faut exécuter le Javascript de la page ou envoyer un formulaire avec un catcha remplis. C'est très embêtant si les développeurs veulent automatiser des requêtes HTTP sur une API ou tout autre site qui peuvent être sous Cloudflare.

## Détecter le mode Under Attack de Cloudflare

Une simple requête HTTP sur un site protégé par Cloudflare en mode "Under Attack" vous retournera une réponse avec un statut "503 Service Unavailable"

| Spécification | Titre |
| ------ | ------ |
| [RFC 7231, section 6.6.4: 503 Service Unavailable](http://tools.ietf.org/html/7231#section-6.6.4) | Hypertext Transfer Protocol (HTTP/1.1): Semantics and Content |

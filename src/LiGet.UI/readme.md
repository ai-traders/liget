# LiGet Frontend

## Developing

1. Start the LiGet backend:

```
cd src/LiGet
dotnet run
```

2. Launch the LiGet frontend:

```
cd src/LiGet.UI
yarn install
yarn develop
```

TODO: backend URLs in "develop" mode are broken. You will need to update `components/SearchResults.tsx`
and `components/DisplayPackage.tsx` to point to your NuGet backend.

## Building

1. Delete `src/LiGet/wwwroot`
2. Rebuild the frontend:

```
cd src/LiGet.UI
yarn build
```
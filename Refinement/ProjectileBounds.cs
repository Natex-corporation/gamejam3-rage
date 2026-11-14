using UnityEngine;

namespace RageRefinement
{
    public static class ProjectileBounds
    {
        private static Camera gameplayCamera;

        // Use the gameplay camera, not Renderer.isVisible (which can include other cameras).
        // Keep a shot until its whole collider has left the viewport.
        public static bool IsOutsideView(Component projectile)
        {
            if (gameplayCamera == null || !gameplayCamera.isActiveAndEnabled)
                gameplayCamera = Camera.main;
            if (gameplayCamera == null) return false; // Original lifetime remains the fallback.

            var collider = projectile.GetComponent<Collider2D>();
            var bounds = collider != null ? collider.bounds : new Bounds(projectile.transform.position, Vector3.zero);
            float minX = float.PositiveInfinity, minY = float.PositiveInfinity;
            float maxX = float.NegativeInfinity, maxY = float.NegativeInfinity;
            float nearest = float.PositiveInfinity, farthest = float.NegativeInfinity;
            for (int corner = 0; corner < 4; corner++)
            {
                var world = new Vector3((corner & 1) == 0 ? bounds.min.x : bounds.max.x,
                    (corner & 2) == 0 ? bounds.min.y : bounds.max.y, bounds.center.z);
                var point = gameplayCamera.WorldToViewportPoint(world);
                minX = Mathf.Min(minX, point.x);
                maxX = Mathf.Max(maxX, point.x);
                minY = Mathf.Min(minY, point.y);
                maxY = Mathf.Max(maxY, point.y);
                nearest = Mathf.Min(nearest, point.z);
                farthest = Mathf.Max(farthest, point.z);
            }
            return maxX < 0f || minX > 1f || maxY < 0f || minY > 1f
                || farthest < gameplayCamera.nearClipPlane || nearest > gameplayCamera.farClipPlane;
        }
    }
}

-- ============================================================
-- CLEANUP DUPLICATE ACCOUNTS (Same email, different auth providers)
-- Schema: identity.* | Tables use snake_case
-- Run this ONCE after deploying the fix
-- ============================================================

-- Step 1: Preview duplicates
SELECT email, COUNT(*) as account_count,
       array_agg(id) as user_ids,
       array_agg(auth_provider) as providers,
       array_agg(status) as statuses,
       array_agg(is_activated) as activated
FROM identity.app_users
GROUP BY email
HAVING COUNT(*) > 1;

-- Step 2: Preview what will be merged/deleted
-- KEEP = oldest activated account | DELETE = newer duplicate
SELECT d.id as delete_id, d.email, d.auth_provider as del_provider, d.created_at as del_created,
       k.id as keep_id, k.auth_provider as keep_provider, k.created_at as keep_created
FROM identity.app_users d
JOIN identity.app_users k ON k.email = d.email AND k.id != d.id
WHERE d.created_at > k.created_at
  AND k.is_activated = true;

-- Step 3: Copy Google OAuth info to kept account (if needed)
UPDATE identity.app_users k
SET auth_provider = d.auth_provider,
    external_user_id = d.external_user_id,
    profile_picture_url = COALESCE(k.profile_picture_url, d.profile_picture_url)
FROM identity.app_users d
WHERE d.email = k.email
  AND d.id != k.id
  AND d.created_at > k.created_at
  AND k.is_activated = true
  AND d.auth_provider = 'Google'
  AND k.auth_provider = 'Local';

-- Step 4: Move store accesses from duplicate to kept account
UPDATE identity.user_store_access usa
SET user_id = k.id
FROM identity.app_users d
JOIN identity.app_users k ON k.email = d.email AND k.id != d.id 
     AND k.created_at < d.created_at AND k.is_activated = true
WHERE usa.user_id = d.id
  AND NOT EXISTS (
    SELECT 1 FROM identity.user_store_access existing
    WHERE existing.user_id = k.id AND existing.store_id = usa.store_id
  );

-- Step 5: Delete roles of duplicate accounts
DELETE FROM identity.user_roles
WHERE user_id IN (
    SELECT d.id FROM identity.app_users d
    JOIN identity.app_users k ON k.email = d.email AND k.id != d.id
    WHERE d.created_at > k.created_at AND k.is_activated = true
);

-- Step 6: Delete remaining store accesses of duplicates
DELETE FROM identity.user_store_access
WHERE user_id IN (
    SELECT d.id FROM identity.app_users d
    JOIN identity.app_users k ON k.email = d.email AND k.id != d.id
    WHERE d.created_at > k.created_at AND k.is_activated = true
);

-- Step 7: Delete duplicate accounts
DELETE FROM identity.app_users
WHERE id IN (
    SELECT d.id FROM identity.app_users d
    JOIN identity.app_users k ON k.email = d.email AND k.id != d.id
    WHERE d.created_at > k.created_at AND k.is_activated = true
);

-- Step 8: Verify cleanup
SELECT email, COUNT(*) FROM identity.app_users GROUP BY email HAVING COUNT(*) > 1;

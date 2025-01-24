#pragma once

#include "defs.h"
#include "config.h"

enum PCA_FLAGS {
	PCA_SCALE_TO_UNITY = 1
};

struct pcainfo {
	int32_t n, m, npcs, flags;
};

extern "C" WCEXPORT int pca_fit(pcainfo* pca, const void** data, const void* allow, void* pcs, void* var);
extern "C" WCEXPORT int pca_data_to_scores(pcainfo* pca, const void* data, const void* pcs, void* scores);
extern "C" WCEXPORT int pca_scores_to_data(pcainfo* pca, const void* scores, const void* pcs, void* data);